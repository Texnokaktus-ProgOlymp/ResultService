namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ScoreAdjustment
{
    public required int Id { get; init; }
    public required decimal Adjustment { get; init; }
    public string? Comment { get; init; }
}
