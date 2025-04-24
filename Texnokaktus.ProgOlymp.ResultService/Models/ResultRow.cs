namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ResultRow(Participant Participant,
                        IDictionary<string, ProblemResult<Score>> ProblemResults,
                        decimal TotalScore);
