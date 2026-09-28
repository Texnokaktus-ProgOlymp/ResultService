using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Results;
using Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Models;

namespace Texnokaktus.ProgOlymp.ResultService.IntegrationTests.Assertions;

internal static class ContestResultsAssert
{
    public static void Matches(ContestResults actual, ExpectedContestResults expected)
    {
        using var scope = Assert.EnterMultipleScope();

        AssertUnordered(actual.Problems, expected.Problems, problem => problem.Id, problem => problem.Id,
            (problem, reference, path) =>
            {
                Assert.That(problem.Alias, Is.EqualTo(reference.Alias), $"{path}.Alias");
                Assert.That(problem.Name, Is.EqualTo(reference.Name), $"{path}.Name");
            }, "Problems");

        AssertUnordered(actual.ResultGroups, expected.ResultGroups, group => group.Name, group => group.Name,
            (group, reference, path) => AssertOrdered(group.Rows, reference.Rows, AssertRow, $"{path}.Rows"),
            "ResultGroups");
    }

    private static void AssertRow(ResultRow actual, ExpectedResultRow expected, string path)
    {
        path += $" (participant {expected.ParticipantId})";
        Assert.That(actual.ParticipantId, Is.EqualTo(expected.ParticipantId), $"{path}.ParticipantId");
        Assert.That(actual.Place, Is.EqualTo(expected.Place), $"{path}.Place");
        Assert.That(actual.IsDisqualified, Is.EqualTo(expected.IsDisqualified), $"{path}.IsDisqualified");
        Assert.That((decimal?)actual.TotalScore, Is.EqualTo(expected.TotalScore), $"{path}.TotalScore");
        AssertOrdered(actual.Results, expected.Results, AssertProblemResult, $"{path}.Results");
    }

    private static void AssertProblemResult(ProblemResult actual, ExpectedProblemResult expected, string path)
    {
        path += $" (problem {expected.ProblemId})";
        Assert.That(actual.ProblemId, Is.EqualTo(expected.ProblemId), $"{path}.ProblemId");

        if (expected.BaseScore is null)
        {
            Assert.That(expected.Adjustments, Is.Empty, $"{path}: an expected missing score cannot have adjustments");
            Assert.That(actual.Score, Is.Null, $"{path}.Score");
            return;
        }

        Assert.That(actual.Score, Is.Not.Null, $"{path}.Score");
        if (actual.Score is not { } score)
            return;

        Assert.That((decimal?)score.BaseScore, Is.EqualTo(expected.BaseScore), $"{path}.Score.BaseScore");
        Assert.That((decimal?)score.AdjustmentsSum, Is.EqualTo(expected.AdjustmentsSum), $"{path}.Score.AdjustmentsSum");
        Assert.That((decimal?)score.TotalScore, Is.EqualTo(expected.TotalScore), $"{path}.Score.TotalScore");
        AssertOrdered(score.Adjustments, expected.Adjustments, AssertAdjustment, $"{path}.Score.Adjustments");
    }

    private static void AssertAdjustment(ScoreAdjustment actual, ExpectedScoreAdjustment expected, string path)
    {
        Assert.That(actual.Id, Is.EqualTo(expected.Id), $"{path}.Id");
        Assert.That((decimal?)actual.Adjustment, Is.EqualTo(expected.Adjustment), $"{path}.Adjustment");
        Assert.That(actual.Comment, Is.EqualTo(expected.Comment), $"{path}.Comment");
    }

    private static void AssertOrdered<TActual, TExpected>(
        IReadOnlyList<TActual> actual,
        IReadOnlyList<TExpected> expected,
        Action<TActual, TExpected, string> assertItem,
        string path)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count), $"{path}.Count");
        for (var index = 0; index < Math.Min(actual.Count, expected.Count); index++)
            assertItem(actual[index], expected[index], $"{path}[{index}]");
    }

    private static void AssertUnordered<TActual, TExpected, TKey>(
        IReadOnlyList<TActual> actual,
        IReadOnlyList<TExpected> expected,
        Func<TActual, TKey> actualKey,
        Func<TExpected, TKey> expectedKey,
        Action<TActual, TExpected, string> assertItem,
        string path)
    {
        var actualKeys = actual.Select(actualKey).ToArray();
        var expectedKeys = expected.Select(expectedKey).ToArray();
        Assert.That(actual.Count, Is.EqualTo(expected.Count), $"{path}.Count");
        Assert.That(actualKeys, Is.Unique, $"{path}: actual keys");
        Assert.That(expectedKeys, Is.Unique, $"{path}: expected keys");
        Assert.That(actualKeys, Is.EquivalentTo(expectedKeys), $"{path}: keys");

        var actualByKey = actual.ToLookup(actualKey);
        foreach (var reference in expected)
        {
            var key = expectedKey(reference);
            var matches = actualByKey[key].ToArray();
            if (matches.Length == 1)
                assertItem(matches[0], reference, $"{path}[{key}]");
        }
    }
}
