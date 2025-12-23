namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ContestResults
{
    public required bool Published { get; init; }
    public required IReadOnlyCollection<Problem> Problems { get; init; }
    public required IReadOnlyCollection<ResultGroup> ResultGroups { get; init; }
}
