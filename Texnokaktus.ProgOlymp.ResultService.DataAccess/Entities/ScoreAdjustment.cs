namespace Texnokaktus.ProgOlymp.ResultService.DataAccess.Entities;

public record ScoreAdjustment
{
    public int Id { get; init; }
    public required decimal Adjustment { get; init; }
    public required string? Comment { get; init; }
}
