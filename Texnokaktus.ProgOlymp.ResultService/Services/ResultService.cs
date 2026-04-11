using Microsoft.EntityFrameworkCore;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Participants;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Context;
using Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;
using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Services.Abstractions;
using DisqualificationNote = Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities.DisqualificationNote;
using Problem = Texnokaktus.ProgOlymp.ResultService.Domain.Problem;
using ProblemResult = Texnokaktus.ProgOlymp.ResultService.Domain.ProblemResult;
using ScoreAdjustment = Texnokaktus.ProgOlymp.ResultService.Domain.ScoreAdjustment;

namespace Texnokaktus.ProgOlymp.ResultService.Services;

public class ResultService(AppDbContext context, ParticipantService.ParticipantServiceClient participantServiceClient) : IResultService
{
    public async Task<ContestResults?> GetResultsAsync(string contestName, ContestStage stage, CancellationToken cancellationToken)
    {
        var contestResult = await context.ContestResults
                                         .AsNoTracking()
                                         .AsSplitQuery()
                                         .Include(contestResult => contestResult.DisqualificationNotes)
                                         .Include(contestResult => contestResult.Problems.OrderBy(problem => problem.Alias))
                                         .ThenInclude(problem => problem.Results)
                                         .FirstOrDefaultAsync(contestResult => contestResult.ContestName == contestName
                                                                            && contestResult.Stage == stage,
                                                              cancellationToken);

        if (contestResult is null) return null;

        var results = from problem in contestResult.Problems
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
                                       ? new ResultScore
                                       {
                                           BaseScore = problemResult.BaseScore,
                                           Adjustments = problemResult.Adjustments
                                                                      .Select(adjustment => new ScoreAdjustment
                                                                       {
                                                                           Id = adjustment.Id,
                                                                           Adjustment = adjustment.Adjustment,
                                                                           Comment = adjustment.Comment
                                                                       })
                                                                      .ToList()
                                       }
                                       : null
                      }
                      group result by result.ParticipantId
                      into grouping
                      select new
                      {
                          ParticipantId = grouping.Key,
                          Results = grouping.Select(arg => new ProblemResult
                          {
                              ProblemId = arg.ProblemId,
                              Alias = arg.ProblemAlias,
                              Score = arg.Result
                          })
                      };

        var participantsResponse = await participantServiceClient.GetContestParticipantsAsync(new()
                                                                                              {
                                                                                                  ContestName = contestName
                                                                                              },
                                                                                              cancellationToken: cancellationToken);

        return new()
        {
            Published = contestResult.Published,
            Problems = contestResult.Problems
                                    .Select(problem => new Problem
                                     {
                                         Id = problem.Id,
                                         Alias = problem.Alias,
                                         Name = problem.Name
                                     })
                                    .ToArray(),
            ResultGroups = participantsResponse.ParticipantGroups
                                               .Select(participantGroup => new ResultGroup
                                                {
                                                    Name = participantGroup.Name,
                                                    Rows = participantGroup.Participants
                                                                           .LeftJoin(contestResult.DisqualificationNotes,
                                                                                     participant => participant.Id,
                                                                                     disqualificationNote => disqualificationNote.ParticipantId,
                                                                                     (participant, disqualificationNote) => new
                                                                                     {
                                                                                         Participant = participant.MapParticipant(),
                                                                                         DisqualificationNote = disqualificationNote
                                                                                     })
                                                                           .Join(results,
                                                                                 arg => arg.Participant.Id,
                                                                                 arg => arg.ParticipantId,
                                                                                 (left, right) => new ResultRow
                                                                                 {
                                                                                     Participant = left.Participant,
                                                                                     ProblemResults = right.Results.ToArray(),
                                                                                     DisqualificationNote = left.DisqualificationNote?.MapDisqualificationNote()
                                                                                 })
                                                                           .OrderBy(row => row.DisqualificationNote is not null)
                                                                           .ThenByDescending(row => row.TotalScore)
                                                                           .RankBy(row => row.DisqualificationNote is null,
                                                                                   row => row.TotalScore ?? 0m)
                                                                           .ToArray()
                                                })
                                               .ToArray()
        };
    }
}

file static class MappingExtensions
{
    public static IEnumerable<RankedItem<TSource>> RankBy<TSource>(this IEnumerable<TSource> source,
                                                                   Func<TSource, bool> rankingCondition,
                                                                   Func<TSource, decimal> selector)
    {
        using var enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext()) yield break;

        var previousPlace = 1;
        var place = 1;
        var previousScore = selector.Invoke(enumerator.Current);

        yield return new(previousPlace, enumerator.Current);

        while (enumerator.MoveNext())
        {
            if (!rankingCondition.Invoke(enumerator.Current))
            {
                yield return new(null, enumerator.Current);
                continue;
            }

            var currentScore = selector.Invoke(enumerator.Current);

            place++;

            previousPlace = previousScore.CompareTo(currentScore) switch
            {
                < 0 => throw new InvalidOperationException(),
                > 0 => place,
                _   => previousPlace
            };

            previousScore = currentScore;

            yield return new(previousPlace, enumerator.Current);
        }
    }

    public static Domain.Participant MapParticipant(this Common.Contracts.Grpc.Participants.Participant participant) =>
        new(participant.Id, participant.Name.MapName(), participant.Grade);

    private static string MapName(this Name name) =>
        string.Join(" ", new[] { name.LastName, name.FirstName, name.Patronym }.Where(namePart => namePart is not null));

    public static Domain.DisqualificationNote MapDisqualificationNote(this DisqualificationNote disqualificationNote) =>
        new(disqualificationNote.Reason);
}
