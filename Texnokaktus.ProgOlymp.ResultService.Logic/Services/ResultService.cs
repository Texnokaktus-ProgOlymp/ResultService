using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Services.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Services;

internal class ResultService(IParticipantService participantService, AppDbContext context) : IResultService
{
    public async Task<ContestResults?> GetResultsAsync(int contestId, DataAccess.Entities.ContestStage stage)
    {
        var contestResult = await context.ContestResults
                                         .AsSplitQuery()
                                         .Include(contestResult => contestResult.Problems)
                                         .ThenInclude(problem => problem.Results)
                                         .ThenInclude(result => result.Adjustments)
                                         .Where(contestResult => contestResult.ContestId == contestId
                                                              && contestResult.Stage == stage)
                                         .FirstOrDefaultAsync();

        if (contestResult is null) return null;

        var results = (from problemId in contestResult.Problems.Select(problem => problem.Id)
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
                                        ? new ResultScore(problemResult.BaseScore,
                                                          problemResult.Adjustments
                                                                       .Select(adjustment => new ScoreAdjustment(adjustment.Id,
                                                                                                                 adjustment.Adjustment,
                                                                                                                 adjustment.Comment))
                                                                       .ToList())
                                        : null
                       }
                       group result by result.ParticipantId
                       into grouping
                       select new
                       {
                           ParticipantId = grouping.Key,
                           Results = grouping.Select(arg => new ProblemResult(arg.ProblemId, arg.Result))
                       }).ToArray();

        var contestData = await participantService.GerParticipantGroups(contestId);

        return new(contestData.Name,
                   contestResult.Problems
                                .Select(problem => new Problem(problem.Id,
                                                               problem.Alias,
                                                               problem.Name))
                                .ToArray(),
                   contestData.ParticipantGroups
                              .Select(participantGroup => new ResultGroup(participantGroup.Name,
                                                                          participantGroup.Participants
                                                                                          .Join(results,
                                                                                                participant => participant.Id,
                                                                                                arg => arg.ParticipantId,
                                                                                                (participant, arg) => new ResultRow(participant, arg.Results.ToArray()))
                                                                                          .OrderByDescending(row => row.TotalScore)
                                                                                          .ToArray()))
                              .ToArray());
    }
}
