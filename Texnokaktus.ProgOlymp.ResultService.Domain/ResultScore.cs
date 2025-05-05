namespace Texnokaktus.ProgOlymp.ResultService.Domain;

public record ResultScore(decimal BaseScore, IReadOnlyCollection<ScoreAdjustment> Adjustments)
{
    public decimal? AdjustmentsSum => Adjustments.Count != 0
                                          ? Adjustments.Sum(adjustment => adjustment.Adjustment)
                                          : null;

    public decimal TotalScore => BaseScore + (AdjustmentsSum ?? 0);
}
