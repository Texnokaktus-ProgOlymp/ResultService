namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ContestResults(string ContestName, IEnumerable<Problem> Problems, IEnumerable<ResultGroup> ResultGroups);
