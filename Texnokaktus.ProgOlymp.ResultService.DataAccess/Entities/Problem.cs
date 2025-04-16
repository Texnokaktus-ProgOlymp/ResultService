namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record Problem
{
    public int Id { get; init; }
    public int ContestResultId { get; init; }
    public string Alias { get; init; }
    public string Name { get; init; }
    public ICollection<ProblemResult> Results { get; init; }
}
