using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Cqrs;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Logic.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Logic.Queries.Handlers;

internal class FullResultQueryHandler(IQueryHandler<ContestParticipantsQuery, IEnumerable<ParticipantGroup>> contestParticipantsQueryHandler, AppDbContext context) : IQueryHandler<FullResultQuery, ContestResults?>
{
    public async Task<ContestResults?> HandleAsync(FullResultQuery query, CancellationToken cancellationToken = default)
    {
        var contestResult = await context.ContestResults
                                         .AsSplitQuery()
                                         .Include(contestResult => contestResult.Problems)
                                         .ThenInclude(problem => problem.Results)
                                         .ThenInclude(result => result.Adjustments)
                                         .Where(contestResult => contestResult.ContestId == query.ContestId
                                                              && contestResult.Stage == query.Stage)
                                         .FirstOrDefaultAsync(cancellationToken);

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
        
        var participantGroups = await contestParticipantsQueryHandler.HandleAsync(new(query.ContestId), cancellationToken);

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
                                                                                                .WithPlaces()
                                                                                                .ToArray()))
                                    .ToArray());
    }
}

file static class MappingExtensions
{
    public static IEnumerable<ResultRow> WithPlaces(this IEnumerable<ResultRow> source)
    {
        var previousPlace = 0;
        var place = 0;
        decimal? previousScore = null;

        foreach (var row in source)
        {
            if (!previousScore.HasValue)
            {
                place++;
                previousScore = row.TotalScore;
                yield return row.WithPlace(++previousPlace);
            }
            else if (previousScore == row.TotalScore)
            {
                place++;
                yield return row.WithPlace(previousPlace);
            }
            else
            {
                place++;
                previousPlace = place;
                previousScore = row.TotalScore;
                yield return row.WithPlace(previousPlace);
            }
        }
    }
}
