using Microsoft.AspNetCore.Http.HttpResults;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Extensions;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries;
using Texnokaktus.ProgOlymp.ResultService.Models;
using ContestResults = Texnokaktus.ProgOlymp.ResultService.Models.ContestResults;
using ContestStage = Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities.ContestStage;
using Participant = Texnokaktus.ProgOlymp.ResultService.Models.Participant;
using Problem = Texnokaktus.ProgOlymp.ResultService.Models.Problem;
using ResultGroup = Texnokaktus.ProgOlymp.ResultService.Models.ResultGroup;
using ResultRow = Texnokaktus.ProgOlymp.ResultService.Models.ResultRow;
using ScoreAdjustment = Texnokaktus.ProgOlymp.ResultService.Models.ScoreAdjustment;

namespace Texnokaktus.ProgOlymp.ResultService.Endpoints;

public static class ResultEndpoints
{
    public static IEndpointRouteBuilder MapResultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/contests/{contestId:int}/{stage}/results");

        group.MapGet("/",
                     async Task<Results<Ok<ContestResults>, NotFound>> (int contestId, Models.ContestStage stage, IQueryHandler<FullResultQuery, Domain.ContestResults?> resultQueryHandler) =>
                     {
                         if (await resultQueryHandler.HandleAsync(new(contestId, stage.MapContestStage())) is not { Published: true } contestResults)
                             return TypedResults.NotFound();

                         var results = new ContestResults(contestResults.Problems.Select(problem => problem.MapProblem()),
                                                          contestResults.ResultGroups.Select(resultGroup => resultGroup.MapResultGroup()));

                         return TypedResults.Ok(results);
                     });

        group.MapGet("/personal",
                     async Task<Results<Ok<ParticipantResult>, NotFound>>(int contestId, Models.ContestStage stage, IQueryHandler<FullResultQuery, Domain.ContestResults?> resultQueryHandler,
                                                                          IQueryHandler<ParticipantIdQuery, int?> participantIdHandler, HttpContext context) =>
                     {
                         if (await participantIdHandler.HandleAsync(new(contestId, context.GetUserId())) is not { } participantId
                          || await resultQueryHandler.HandleAsync(new(contestId, stage.MapContestStage())) is not { Published: true } contestResults
                          || contestResults.ResultGroups
                                           .SelectMany(resultGroup => resultGroup.Rows.Select(row => new { Group = resultGroup.Name, Row = row }))
                                           .FirstOrDefault(row => row.Row.Participant.Id == participantId) is not { } resultRow)
                             return TypedResults.NotFound();

                         var result = new ParticipantResult(resultRow.Row.Place,
                                                            resultRow.Group,
                                                            contestResults.Problems.Select(problem => problem.MapProblem()),
                                                            resultRow.Row.ProblemResults.ToDictionary(problemResult => problemResult.Alias,
                                                                                                      problemResult => problemResult.MapProblemResult(score => score.MapExtendedScore())),
                                                            resultRow.Row.TotalScore);

                         return TypedResults.Ok(result);
                     })
             .RequireAuthorization();

        return app;
    }
}

file static class MappingExtensions
{
    public static Problem MapProblem(this Domain.Problem problem) => new(problem.Alias, problem.Name);

    public static ResultGroup MapResultGroup(this Domain.ResultGroup resultGroup) =>
        new(resultGroup.Name, resultGroup.Rows.Select(row => row.MapResultRow()));

    private static ResultRow MapResultRow(this Domain.ResultRow resultRow) =>
        new(resultRow.Place,
            resultRow.Participant.MapParticipant(),
            resultRow.ProblemResults.ToDictionary(problemResult => problemResult.Alias,
                                                  problemResult => problemResult.MapProblemResult(score => score.MapScore())),
            resultRow.TotalScore);

    private static Score MapScore(this ResultScore resultScore) =>
        new(resultScore.BaseScore,
            resultScore.AdjustmentsSum,
            resultScore.TotalScore);

    public static ExtendedScore MapExtendedScore(this ResultScore resultScore) =>
        new(resultScore.BaseScore,
            resultScore.Adjustments.Select(scoreAdjustment => scoreAdjustment.MapScoreAdjustment()),
            resultScore.AdjustmentsSum,
            resultScore.TotalScore);

    private static ScoreAdjustment MapScoreAdjustment(this Domain.ScoreAdjustment scoreAdjustment) =>
        new(scoreAdjustment.Adjustment, scoreAdjustment.Comment);

    private static Participant MapParticipant(this Domain.Participant participant) =>
        new(participant.Name, participant.Grade);

    public static ProblemResult<TScore> MapProblemResult<TScore>(this ProblemResult problemResult, Func<ResultScore, TScore> resultScoreMapper) where TScore : class =>
        new(problemResult.Score is { } score
            ? resultScoreMapper.Invoke(score)
            : null);
    
    public static ContestStage MapContestStage(this Models.ContestStage stage) => stage switch
    {
        Models.ContestStage.Preliminary => ContestStage.Preliminary,
        Models.ContestStage.Final       => ContestStage.Final,
        _                               => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
}
