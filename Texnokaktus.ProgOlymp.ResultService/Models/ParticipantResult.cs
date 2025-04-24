namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ParticipantResult(IEnumerable<Problem> Problems,
                                IDictionary<string, ProblemResult<ExtendedScore>> ProblemResults,
                                decimal TotalScore);
