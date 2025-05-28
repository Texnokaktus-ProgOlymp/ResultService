using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Logic.Commands;
using Texnokaktus.ProgOlymp.ResultService.Logic.Exceptions.Rpc;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries;
using Contest = Texnokaktus.ProgOlymp.ResultService.Domain.Contest;
using ContestResults = Texnokaktus.ProgOlymp.ResultService.Domain.ContestResults;
using ContestStage = Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities.ContestStage;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

public class ResultServiceImpl(ICommandHandler<CreateContestCommand> createContestHandler,
                               IQueryHandler<ContestQuery, Contest> getContestHandler,
                               ICommandHandler<CreateProblemCommand> createProblemHandler,
                               ICommandHandler<CreateResultCommand> createResultHandler,
                               ICommandHandler<CreateResultAdjustmentCommand, int> createResultAdjustmentHandler,
                               IQueryHandler<FullResultQuery, ContestResults?> resultQueryHandler)
    : Common.Contracts.Grpc.Results.ResultService.ResultServiceBase
{
    public override async Task<Common.Contracts.Grpc.Results.Contest> GetContest(GetContestRequest request, ServerCallContext context)
    {
        var contest = await getContestHandler.HandleAsync(new(request.ContestId, request.Stage.MapContestStage()));

        return new()
        {
            Id = contest.Id,
            Stage = contest.Stage.MapContestStage(),
            StageId = contest.StageId,
            Problems =
            {
                contest.Problems.Select(problem => new Problem
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
        await createContestHandler.HandleAsync(new(request.Id, request.Stage.MapContestStage(), request.StageId), context.CancellationToken);

        return new();
    }

    public override async Task<Empty> AddProblem(AddProblemRequest request, ServerCallContext context)
    {
        await createProblemHandler.HandleAsync(new(request.ContestId, request.Stage.MapContestStage(), request.Alias, request.Name), context.CancellationToken);

        return new();
    }

    public override async Task<Common.Contracts.Grpc.Results.ContestResults> GetResults(GetResultsRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResults = await resultQueryHandler.HandleAsync(new(request.ContestId, stage), context.CancellationToken)
                          ?? throw new ContestNotFoundException(request.ContestId, stage);

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
                                                       Place = resultRow.Place,
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

        var contestResults = await resultQueryHandler.HandleAsync(new(request.ContestId, stage), context.CancellationToken)
                          ?? throw new ContestNotFoundException(request.ContestId, stage);

        if (contestResults.ResultGroups
                          .SelectMany(resultGroup => resultGroup.Rows.Select(row => new { Group = resultGroup.Name, Row = row }))
                          .FirstOrDefault(row => row.Row.Item.Participant.Id == request.ParticipantId) is not { } resultRow)
            throw new ParticipantResultsNotFoundException(request.ContestId, stage, request.ParticipantId);

        return new()
        {
            Place = resultRow.Row.Place,
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
        await createResultHandler.HandleAsync(new(request.ContestId, request.Stage.MapContestStage(), request.Alias, request.ParticipantId, request.BaseScore), context.CancellationToken);

        return new();
    }

    public override async Task<AddResultAdjustmentResponse> AddResultAdjustment(AddResultAdjustmentRequest request, ServerCallContext context)
    {
        var id = await createResultAdjustmentHandler.HandleAsync(new(request.ContestId, request.Stage.MapContestStage(), request.Alias, request.ParticipantId, request.Adjustment, request.Comment), context.CancellationToken);

        return new()
        {
            Id = id
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
