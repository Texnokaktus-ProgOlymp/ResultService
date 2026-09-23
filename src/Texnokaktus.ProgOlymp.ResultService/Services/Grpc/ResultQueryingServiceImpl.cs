using Grpc.Core;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.Exceptions.Rpc;
using Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;
using ContestStage = Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities.ContestStage;

namespace Texnokaktus.ProgOlymp.ResultService.Services.Grpc;

public class ResultQueryingServiceImpl(IResultService resultService) : ResultQueryingService.ResultQueryingServiceBase
{
    public override async Task<ContestResults> GetResults(GetResultsRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResults = await resultService.GetResultsAsync(request.ContestName, stage, context.CancellationToken)
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
                                                       TotalScore = resultRow.Item.TotalScore,
                                                       IsDisqualified = resultRow.Item.DisqualificationNote is not null
                                                   })
                                   }
                               })
            }
        };
    }

    public override async Task<ParticipantResults> GetResultsByParticipant(GetResultsByParticipantRequest request, ServerCallContext context)
    {
        var stage = request.Stage.MapContestStage();

        var contestResults = await resultService.GetResultsAsync(request.ContestName, stage, context.CancellationToken)
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
}

file static class MappingExtensions
{
    public static ContestStage MapContestStage(this Common.Contracts.Grpc.Results.ContestStage contestStage) =>
        contestStage switch
        {
            Common.Contracts.Grpc.Results.ContestStage.Preliminary => ContestStage.Preliminary,
            Common.Contracts.Grpc.Results.ContestStage.Final       => ContestStage.Final,
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

