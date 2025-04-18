using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

public class ResultServiceImpl(AppDbContext dbContext) : Common.Contracts.Grpc.Results.ResultService.ResultServiceBase
{
    public override async Task<Contest> GetContest(GetContestRequest request, ServerCallContext context)
    {
        var contestStage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .AsNoTracking()
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestId == request.ContestId
                                                                       && result.Stage == contestStage)
                         ?? throw new ContestNotFoundException(request.ContestId, contestStage);

        return new()
        {
            Id = contestResult.ContestId,
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

        if (await dbContext.ContestResults.AnyAsync(contestResult => contestResult.ContestId == request.Id
                                                                  && contestResult.Stage == contestStage,
                                                    context.CancellationToken))
            throw new ContestAlreadyExistsException(request.Id, contestStage);

        if (await dbContext.ContestResults.AnyAsync(result => result.StageId == request.StageId,
                                                    context.CancellationToken))
            throw new ContestAlreadyExistsException(request.StageId);

        dbContext.ContestResults.Add(new()
        {
            ContestId = request.Id,
            Stage = contestStage,
            StageId = request.StageId
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);

        return new();
    }

    public override async Task<Empty> AddProblem(AddProblemRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .Include(result => result.Problems)
                                           .FirstOrDefaultAsync(result => result.ContestId == request.ContestId
                                                                       && result.Stage == stage)
                         ?? throw new ContestNotFoundException(request.ContestId, stage);

        if (contestResult.Published)
            throw new ContestReadonlyException(request.ContestId, stage);

        if (contestResult.Problems.Any(problem => problem.Alias == request.Alias))
            throw new ProblemAlreadyExistsException(request.ContestId, stage, request.Alias);

        contestResult.Problems.Add(new()
        {
            Alias = request.Alias,
            Name = request.Name
        });

        await dbContext.SaveChangesAsync();

        return new();
    }

    public override async Task<ContestResults> GetResults(GetResultsRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .AsSplitQuery()
                                           .Include(contestResult => contestResult.Problems)
                                           .ThenInclude(problem => problem.Results)
                                           .ThenInclude(result => result.Adjustments)
                                           .Where(contestResult => contestResult.ContestId == request.ContestId
                                                                && contestResult.Stage == stage)
                                           .FirstOrDefaultAsync()
                         ?? throw new ContestNotFoundException(request.ContestId, stage);

        return new()
        {
            Problems =
            {
                contestResult.Problems.Select(problem => new Problem
                {
                    Id = problem.Id,
                    Alias = problem.Alias,
                    Name = problem.Name
                })
            },
            Rows =
            {
                from problemId in contestResult.Problems.Select(problem => problem.Id)
                from participantId in contestResult.Problems
                                                   .SelectMany(problem => problem.Results.Select(problemResult => problemResult.ParticipantId))
                                                   .Distinct()
                let problemResult = contestResult.Problems.FirstOrDefault(problem => problem.Id == problemId)
                                                ?.Results.FirstOrDefault(result => result.ParticipantId == participantId)
                let result = new
                {
                    ProblemId = problemId,
                    ParticipantId = participantId,
                    Result = problemResult is not null
                                 ? new Domain.ResultScore(problemResult.BaseScore,
                                                          problemResult.Adjustments
                                                                       .Select(adjustment => new Domain.ScoreAdjustment(adjustment.Id,
                                                                                                                        adjustment.Adjustment,
                                                                                                                        adjustment.Comment))
                                                                       .ToList())
                                 : null
                }
                group result by result.ParticipantId
                into grouping
                select new ResultRow
                {
                    ParticipantId = grouping.Key,
                    Results =
                    {
                        grouping.Select(arg => new ProblemResult
                        {
                            ProblemId = arg.ProblemId,
                            Score = arg.Result?.MapResultScore()
                        })
                    }
                }
            }
        };
    }

    public override async Task<ParticipantResults> GetResultsByParticipant(GetResultsByParticipantRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .AsSplitQuery()
                                           .Include(contestResult => contestResult.Problems)
                                           .ThenInclude(problem => problem.Results)
                                           .ThenInclude(result => result.Adjustments)
                                           .Where(contestResult => contestResult.ContestId == request.ContestId
                                                                && contestResult.Stage == stage)
                                           .FirstOrDefaultAsync()
                         ?? throw new ContestNotFoundException(request.ContestId, stage);

        if (contestResult.Problems.SelectMany(problem => problem.Results).All(problemResult => problemResult.ParticipantId != request.ParticipantId))
            throw new ParticipantResultsNotFound(request.ContestId, stage, request.ParticipantId);

        return new()
        {
            Problems =
            {
                contestResult.Problems.Select(problem => new Problem
                {
                    Id = problem.Id,
                    Alias = problem.Alias,
                    Name = problem.Name
                })
            },
            Results =
            {
                from problemId in contestResult.Problems.Select(problem => problem.Id)
                let problemResult = contestResult.Problems.FirstOrDefault(problem => problem.Id == problemId)
                                                ?.Results.FirstOrDefault(result => result.ParticipantId == request.ParticipantId)
                select new ProblemResult
                {
                    ProblemId = problemId,
                    Score = problemResult is not null
                                ? new Domain.ResultScore(problemResult.BaseScore,
                                                         problemResult.Adjustments
                                                                      .Select(adjustment => new Domain.ScoreAdjustment(adjustment.Id,
                                                                                                                       adjustment.Adjustment,
                                                                                                                       adjustment.Comment))
                                                                      .ToList()).MapResultScore()
                                : null
                }
            }
        };
    }

    public override async Task<Empty> AddResult(AddResultRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResult = await dbContext.ContestResults
                                           .Include(contestResult => contestResult.Problems)
                                           .ThenInclude(problem => problem.Results)
                                           .FirstOrDefaultAsync(contestResult => contestResult.ContestId == request.ContestId
                                                                              && contestResult.Stage == stage)
                         ?? throw new ContestNotFoundException(request.ContestId, stage);

        if (contestResult.Published)
            throw new ContestReadonlyException(request.ContestId, stage);

        var problem = contestResult.Problems.FirstOrDefault(problem => problem.Alias == request.Alias)
                   ?? throw new ProblemNotFoundException(request.ContestId, stage, request.Alias);

        if (problem.Results.Any(result => result.ParticipantId == request.ParticipantId))
            throw new ResultAlreadyExistsException(request.ContestId, stage, request.Alias, request.ParticipantId);

        problem.Results.Add(new()
        {
            ParticipantId = request.ParticipantId,
            BaseScore = request.BaseScore
        });

        return new();
    }
}

file static class MappingExtensions
{
    public static DataAccess.Entities.ContestStage MapContestStage(this ContestStage contestStage) =>
        contestStage switch
        {
            ContestStage.Preliminary => DataAccess.Entities.ContestStage.Preliminary,
            ContestStage.Final       => DataAccess.Entities.ContestStage.Final,
            _                        => throw new ArgumentOutOfRangeException(nameof(contestStage), contestStage, null)
        };

    public static ContestStage MapContestStage(this DataAccess.Entities.ContestStage contestStage) =>
        contestStage switch
        {
            DataAccess.Entities.ContestStage.Preliminary => ContestStage.Preliminary,
            DataAccess.Entities.ContestStage.Final => ContestStage.Final,
            _ => throw new ArgumentOutOfRangeException(nameof(contestStage), contestStage, null)
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
