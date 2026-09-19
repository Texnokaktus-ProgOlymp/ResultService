namespace Texnokaktus.ProgOlymp.ResultService.Models;

public record ExtendedScore(decimal BaseScore,
                            IEnumerable<ScoreAdjustment> Adjustments,
                            decimal? AdjustmentsSum,
                            decimal TotalScore);
