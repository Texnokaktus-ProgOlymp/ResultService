namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ParticipantResult(int Place,
                                string Group,
                                IEnumerable<Problem> Problems,
                                IDictionary<string, ProblemResult<ExtendedScore>> ProblemResults,
                                decimal TotalScore);
