namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultScore
{
    public required decimal BaseScore { get; init; }
    public required IReadOnlyCollection<ScoreAdjustment> Adjustments { get; init; }

    public decimal? AdjustmentsSum => Adjustments.Count != 0
                                          ? Adjustments.Sum(adjustment => adjustment.Adjustment)
                                          : null;

    public decimal TotalScore => BaseScore + (AdjustmentsSum ?? 0);
}
