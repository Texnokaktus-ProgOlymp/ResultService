namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultGroup(string Name, IReadOnlyCollection<ResultRow> Rows);
