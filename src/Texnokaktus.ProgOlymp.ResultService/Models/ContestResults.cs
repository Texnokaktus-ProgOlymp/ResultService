namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ContestResults(IEnumerable<Problem> Problems, IEnumerable<ResultGroup> ResultGroups);
