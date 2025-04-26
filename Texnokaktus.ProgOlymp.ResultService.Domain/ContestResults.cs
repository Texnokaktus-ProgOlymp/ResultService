namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ContestResults(bool Published, IReadOnlyCollection<Problem> Problems, IReadOnlyCollection<ResultGroup> ResultGroups);
