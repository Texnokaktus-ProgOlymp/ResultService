namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ResultRow(int Place,
                        Participant Participant,
                        IDictionary<string, ProblemResult<Score>> ProblemResults,
                        decimal TotalScore);
