namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultGroup
{
    public required string Name { get; init; }
    public required IReadOnlyCollection<RankedItem<ResultRow>> Rows { get; init; }
}
