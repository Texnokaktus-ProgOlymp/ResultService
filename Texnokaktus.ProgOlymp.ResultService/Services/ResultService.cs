using Microsoft.AspNetCore.Http.HttpResults;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.Logic.QueryHandlers;
using Texnokaktus.ProgOlymp.ResultService.Models;
using Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Services;

public class ResultService(IQueryHandler<FullResultQuery, Domain.ContestResults?> resultQueryHandler,
                           Logic.Services.Abstractions.IParticipantService participantService) : IResultService
{
    public async Task<Results<Ok<ContestResults>, NotFound>> GetContestResultsAsync(int contestId, ContestStage stage)
    {
        if (await resultQueryHandler.HandleAsync(new(contestId, stage.MapContestStage())) is not { Published: true } contestResults)
            return TypedResults.NotFound();

        var results = new ContestResults(contestResults.Problems.Select(problem => problem.MapProblem()),
                                         contestResults.ResultGroups.Select(group => group.MapResultGroup()));

        return TypedResults.Ok(results);
    }

    public async Task<Results<Ok<ParticipantResult>, NotFound>> GetParticipantResultsAsync(int contestId, ContestStage stage, int userId)
    {
        if (await participantService.GetParticipantIdAsync(contestId, userId) is not { } participantId
         || await resultQueryHandler.HandleAsync(new(contestId, stage.MapContestStage())) is not { Published: true } contestResults
         || contestResults.ResultGroups
                          .SelectMany(group => group.Rows.Select(row => new { Group = group.Name, Row = row }))
                          .FirstOrDefault(row => row.Row.Participant.Id == participantId) is not { } resultRow)
            return TypedResults.NotFound();

        var result = new ParticipantResult(resultRow.Row.Place,
                                           resultRow.Group,
                                           contestResults.Problems.Select(problem => problem.MapProblem()),
                                           resultRow.Row.ProblemResults.ToDictionary(problemResult => problemResult.Alias,
                                                                                     problemResult => problemResult.MapProblemResult(score => score.MapExtendedScore())),
                                           resultRow.Row.TotalScore);

        return TypedResults.Ok(result);
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

    private static Score MapScore(this Domain.ResultScore resultScore) =>
        new(resultScore.BaseScore,
            resultScore.AdjustmentsSum,
            resultScore.TotalScore);

    public static ExtendedScore MapExtendedScore(this Domain.ResultScore resultScore) =>
        new(resultScore.BaseScore,
            resultScore.Adjustments.Select(scoreAdjustment => scoreAdjustment.MapScoreAdjustment()),
            resultScore.AdjustmentsSum,
            resultScore.TotalScore);

    private static ScoreAdjustment MapScoreAdjustment(this Domain.ScoreAdjustment scoreAdjustment) =>
        new(scoreAdjustment.Adjustment, scoreAdjustment.Comment);

    private static Participant MapParticipant(this Domain.Participant participant) =>
        new(participant.Name, participant.Grade);

    public static ProblemResult<TScore> MapProblemResult<TScore>(this Domain.ProblemResult problemResult, Func<Domain.ResultScore, TScore> resultScoreMapper) where TScore : class =>
        new(problemResult.Score is { } score
            ? resultScoreMapper.Invoke(score)
            : null);
    
    public static DataAccess.Entities.ContestStage MapContestStage(this ContestStage stage) => stage switch
    {
        ContestStage.Preliminary => DataAccess.Entities.ContestStage.Preliminary,
        ContestStage.Final       => DataAccess.Entities.ContestStage.Final,
        _                        => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };
}
