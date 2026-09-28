namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

internal record ExpectedProblemResult(int ProblemId, decimal? BaseScore, params ExpectedScoreAdjustment[] Adjustments)
{
    public decimal? AdjustmentsSum => Adjustments.Length == 0 ? null : Adjustments.Sum(adjustment => adjustment.Adjustment);

    public decimal? TotalScore => BaseScore + (AdjustmentsSum ?? 0m);
}
