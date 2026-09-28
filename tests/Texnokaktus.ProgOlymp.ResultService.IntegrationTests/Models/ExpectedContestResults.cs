namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

internal record ExpectedContestResults(
    IReadOnlyList<ExpectedProblem> Problems,
    IReadOnlyList<ExpectedResultGroup> ResultGroups);
