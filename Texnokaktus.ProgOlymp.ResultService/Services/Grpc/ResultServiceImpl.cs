using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;
using ContestStage = Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities.ContestStage;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

public class ResultServiceImpl(AppDbContext dbContext, IFullResultQueryHandler resultQueryHandler) : Common.Contracts.Grpc.Results.ResultService.ResultServiceBase
{
    public override async Task<Contest> GetContest(GetContestRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .AsNoTracking()
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestName == request.ContestName
                                                                       && result.Stage == contestStage,
                                                                context.CancellationToken)
                         ?? throw new ContestNotFoundException(request.ContestName, contestStage);

        return new()
        {
            Id = contestResult.Id,
            Stage = contestResult.Stage.MapContestStage(),
            StageId = contestResult.StageId,
            Problems =
            {
                contestResult.Problems.Select(problem => new Problem
                {
                    Id = problem.Id,
                    Alias = problem.Alias,
                    Name = problem.Name
                })
            }
        };
    }

    public override async Task<Empty> AddContest(AddContestRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        if (await dbContext.ContestResults.AnyAsync(contestResult => contestResult.ContestName == request.ContestName
                                                                  && contestResult.Stage == contestStage,
                                                    context.CancellationToken))
            throw new ContestAlreadyExistsException(request.ContestName, contestStage);

        if (await dbContext.ContestResults.AnyAsync(result => result.StageId == request.StageId,
                                                    context.CancellationToken))
            throw new ContestAlreadyExistsException(request.StageId);

        dbContext.ContestResults.Add(new()
        {
            ContestName = request.ContestName,
            Stage = contestStage,
            StageId = request.StageId
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new();
    }

    public override async Task<Empty> AddProblem(AddProblemRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .Include(result => result.Problems.Where(problem => problem.Alias == request.Alias))
                                           .FirstOrDefaultAsync(result => result.ContestName == request.ContestName
                                                                       && result.Stage == contestStage,
                                                                context.CancellationToken)
                         ?? throw new ContestNotFoundException(request.ContestName, contestStage);

        if (contestResult.Published)
            throw new ContestReadonlyException(request.ContestName, contestStage);

        if (contestResult.Problems.Count != 0)
            throw new ProblemAlreadyExistsException(request.ContestName, contestStage, request.Alias);

        contestResult.Problems.Add(new()
        {
            Alias = request.Alias,
            Name = request.Name
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new();
    }

    public override async Task<ContestResults> GetResults(GetResultsRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResults = await resultQueryHandler.HandleAsync(new(request.ContestName, stage), context.CancellationToken)
                          ?? throw new ContestNotFoundException(request.ContestName, stage);

        return new()
        {
            Problems =
            {
                contestResults.Problems
                              .Select(problem => new Problem
                               {
                                   Id = problem.Id,
                                   Alias = problem.Alias,
                                   Name = problem.Name
                               })
            },
            ResultGroups =
            {
                contestResults.ResultGroups
                              .Select(resultGroup => new ResultGroup
                               {
                                   Name = resultGroup.Name,
                                   Rows =
                                   {
                                       resultGroup.Rows
                                                  .Select(resultRow => new ResultRow
                                                   {
                                                       Place = resultRow.Rank,
                                                       ParticipantId = resultRow.Item.Participant.Id,
                                                       Results =
                                                       {
                                                           resultRow.Item
                                                                    .ProblemResults
                                                                    .Select(result => new ProblemResult
                                                                     {
                                                                         ProblemId = result.ProblemId,
                                                                         Score = result.Score?.MapResultScore()
                                                                     })
                                                       },
                                                       TotalScore = resultRow.Item.TotalScore
                                                   })
                                   }
                               })
            }
        };
    }

    public override async Task<ParticipantResults> GetResultsByParticipant(GetResultsByParticipantRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResults = await resultQueryHandler.HandleAsync(new(request.ContestName, stage), context.CancellationToken)
                          ?? throw new ContestNotFoundException(request.ContestName, stage);

        if (contestResults.ResultGroups
                          .SelectMany(resultGroup => resultGroup.Rows.Select(row => new { Group = resultGroup.Name, Row = row }))
                          .FirstOrDefault(row => row.Row.Item.Participant.Id == request.ParticipantId) is not { } resultRow)
            throw new ParticipantResultsNotFoundException(request.ContestName, stage, request.ParticipantId);

        return new()
        {
            Place = resultRow.Row.Rank,
            ResultGroupName = resultRow.Group,
            Problems =
            {
                contestResults.Problems
                              .Select(problem => new Problem
                               {
                                   Id = problem.Id,
                                   Alias = problem.Alias,
                                   Name = problem.Name
                               })
            },
            Results =
            {
                resultRow.Row
                         .Item
                         .ProblemResults
                         .Select(result => new ProblemResult
                          {
                              ProblemId = result.ProblemId,
                              Score = result.Score?.MapResultScore()
                          })
            },
            TotalScore = resultRow.Row.Item.TotalScore
        };
    }

    public override async Task<Empty> AddResult(AddResultRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .Include(contestResult => contestResult.Problems.Where(problem => problem.Alias == request.Alias))
                                           .ThenInclude(problem => problem.Results.Select(result => result.ParticipantId == request.ParticipantId))
                                           .FirstOrDefaultAsync(contestResult => contestResult.ContestName == request.ContestName
                                                                              && contestResult.Stage == contestStage,
                                                                context.CancellationToken)
                         ?? throw new ContestNotFoundException(request.ContestName, contestStage);

        if (contestResult.Published)
            throw new ContestReadonlyException(request.ContestName, contestStage);

        var problem = contestResult.Problems.FirstOrDefault()
                   ?? throw new ProblemNotFoundException(request.ContestName, contestStage, request.Alias);

        if (problem.Results.Count != 0)
            throw new ResultAlreadyExistsException(request.ContestName, contestStage, request.Alias, request.ParticipantId);

        problem.Results.Add(new()
        {
            ParticipantId = request.ParticipantId,
            BaseScore = request.BaseScore
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new();
    }

    public override async Task<AddResultAdjustmentResponse> AddResultAdjustment(AddResultAdjustmentRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                        .Include(contestResult => contestResult.Problems.Where(problem => problem.Alias == request.Alias))
                                        .ThenInclude(problem => problem.Results.Where(result => result.ParticipantId == request.ParticipantId))
                                        .FirstOrDefaultAsync(contestResult => contestResult.ContestName == request.ContestName
                                                                           && contestResult.Stage == contestStage,
                                                             context.CancellationToken)
                      ?? throw new ContestNotFoundException(request.ContestName, contestStage);

        if (contestResult.Published)
            throw new ContestReadonlyException(request.ContestName, contestStage);

        var problem = contestResult.Problems.FirstOrDefault()
                   ?? throw new ProblemNotFoundException(request.ContestName, contestStage, request.Alias);

        var result = problem.Results.FirstOrDefault()
                  ?? throw new ResultNotFoundException(request.ContestName, contestStage, request.Alias, request.ParticipantId);

        var entity = new DataAccess.Entities.ScoreAdjustment
        {
            Adjustment = request.Adjustment,
            Comment = request.Comment
        };

        result.Adjustments.Add(entity);

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new()
        {
            Id = entity.Id
        };
    }
}

file static class MappingExtensions
{
    public static ContestStage MapContestStage(this Common.Contracts.Grpc.Results.ContestStage contestStage) =>
        contestStage switch
        {
            Common.Contracts.Grpc.Results.ContestStage.Preliminary => ContestStage.Preliminary,
            Common.Contracts.Grpc.Results.ContestStage.Final       => ContestStage.Final,
            _                        => throw new ArgumentOutOfRangeException(nameof(contestStage), contestStage, null)
        };

    public static Common.Contracts.Grpc.Results.ContestStage MapContestStage(this ContestStage contestStage) =>
        contestStage switch
        {
            ContestStage.Preliminary => Common.Contracts.Grpc.Results.ContestStage.Preliminary,
            ContestStage.Final       => Common.Contracts.Grpc.Results.ContestStage.Final,
            _                        => throw new ArgumentOutOfRangeException(nameof(contestStage), contestStage, null)
        };

    public static ResultScore MapResultScore(this Domain.ResultScore resultScore) =>
        new()
        {
            BaseScore = resultScore.BaseScore,
            AdjustmentsSum = resultScore.AdjustmentsSum,
            TotalScore = resultScore.TotalScore,
            Adjustments =
            {
                resultScore.Adjustments.Select(scoreAdjustment => scoreAdjustment.MapScoreAdjustment())
            }
        };

    private static ScoreAdjustment MapScoreAdjustment(this Domain.ScoreAdjustment scoreAdjustment) =>
        new()
        {
            Id = scoreAdjustment.Id,
            Adjustment = scoreAdjustment.Adjustment,
            Comment = scoreAdjustment.Comment
        };
}
