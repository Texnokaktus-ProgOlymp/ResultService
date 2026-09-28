namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

internal record ExpectedResultRow(
    int ParticipantId,
    int? Place,
    IReadOnlyList<ExpectedProblemResult> Results,
    bool IsDisqualified = false)
{
    public decimal? TotalScore => IsDisqualified ? null : Results.Sum(result => result.TotalScore) ?? 0m;
}
