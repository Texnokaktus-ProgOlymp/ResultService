using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers.Abstractions;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

internal class FullResultQueryHandler(IContestParticipantsQueryHandler contestParticipantsQueryHandler, AppDbContext context) : IFullResultQueryHandler
{
    public async Task<ContestResults?> HandleAsync(FullResultQuery query, CancellationToken cancellationToken = default)
    {
        var contestResult = await context.ContestResults
                                         .AsNoTracking()
                                         .AsSplitQuery()
                                         .Include(contestResult => contestResult.Problems.OrderBy(problem => problem.Alias))
                                         .ThenInclude(problem => problem.Results)
                                         .ThenInclude(result => result.Adjustments)
                                         .FirstOrDefaultAsync(contestResult => contestResult.ContestName == query.ContestName
                                                                            && contestResult.Stage == query.Stage,
                                                              cancellationToken);

        if (contestResult is null) return null;

        var results = (from problem in contestResult.Problems
                       from participantId in contestResult.Problems
                                                          .SelectMany(p => p.Results.Select(problemResult => problemResult.ParticipantId))
                                                          .Distinct()
                       let problemResult = contestResult.Problems.FirstOrDefault(p => p.Id == problem.Id)
                                                       ?.Results.FirstOrDefault(result => result.ParticipantId == participantId)
                       let result = new
                       {
                           ProblemId = problem.Id,
                           ProblemAlias = problem.Alias,
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
                           Results = grouping.Select(arg => new ProblemResult(arg.ProblemId,
                                                                              arg.ProblemAlias,
                                                                              arg.Result))
                       }).ToArray();

        var participantGroups = await contestParticipantsQueryHandler.HandleAsync(new(query.ContestName), cancellationToken);

        return new(contestResult.Published,
                   contestResult.Problems
                                .Select(problem => new Problem(problem.Id,
                                                               problem.Alias,
                                                               problem.Name))
                                .ToArray(),
                   participantGroups.Select(participantGroup => new ResultGroup(participantGroup.Name,
                                                                                participantGroup.Participants
                                                                                                .Join(results,
                                                                                                      participant => participant.Id,
                                                                                                      arg => arg.ParticipantId,
                                                                                                      (participant, arg) => new ResultRow(participant, arg.Results.ToArray()))
                                                                                                .OrderByDescending(row => row.TotalScore)
                                                                                                .Rank(row => row.TotalScore)
                                                                                                .ToArray()))
                                    .ToArray());
    }
}

file static class MappingExtensions
{
    public static IEnumerable<RankedItem<TSource>> Rank<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
    {
        var previousPlace = 0;
        var place = 0;
        decimal? previousScore = null;

        foreach (var row in source)
        {
            var currentScore = selector.Invoke(row);

            place++;
            if (!previousScore.HasValue)
                previousPlace++;
            else if (previousScore != currentScore)
                previousPlace = place;

            previousScore = currentScore;

            yield return new(previousPlace, row);
        }
    }
}
