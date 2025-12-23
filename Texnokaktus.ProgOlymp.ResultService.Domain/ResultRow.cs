namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultRow
{
    public required Participant Participant { get; init; }
    public required IReadOnlyCollection<ProblemResult> ProblemResults { get; init; }

    public decimal TotalScore => ProblemResults.Sum(result => result.Score?.TotalScore) ?? 0m;
}
