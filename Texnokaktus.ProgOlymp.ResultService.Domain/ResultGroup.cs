namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultGroup(string Name, IReadOnlyCollection<RankedItem<ResultRow>> Rows);
