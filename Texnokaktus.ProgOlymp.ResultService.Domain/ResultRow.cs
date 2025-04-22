namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultRow(Participant Participant, IReadOnlyCollection<ProblemResult> ProblemResults)
{
    public decimal TotalScore => ProblemResults.Sum(result => result.Score?.TotalScore) ?? 0m;
}
