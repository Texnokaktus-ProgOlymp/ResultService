namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ContestResults(string ContestName, IReadOnlyCollection<Problem> Problems, IReadOnlyCollection<ResultGroup> ResultGroups);
