namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ScoreAdjustment
{
    public int Id { get; init; }
    public int ProblemResultId { get; init; }
    public decimal Adjustment { get; init; }
    public string? Comment { get; init; }
}
