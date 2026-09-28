using Texnokaktus.ProgOlymp.ResultService.Domain;
using Texnokaktus.ProgOlymp.ResultService.Extensions;

namespace Texnokaktus.ProgOlymp.ResultService.Tests;

public class RankingExtensionsTests
{
    [Test]
    public void EmptySource_ReturnsEmptyResult([Values] bool condition)
    {
        var source = Enumerable.Empty<int>();

        var result = source.RankBy(_ => condition, Comparer<int>.Default);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void DifferentValues_DifferentRanks()
    {
        List<int> source = [10, 5, 2];

        var result = source.RankBy(_ => true, Comparer<int>.Default);

        List<RankedItem<int>> expected = [new(Rank: 1, Item: 10), new(Rank: 2, Item: 5), new(Rank: 3, Item: 2)];
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void SameValues_SameRank()
    {
        List<int> source = [10, 10, 10];

        var result = source.RankBy(_ => true, Comparer<int>.Default);

        List<RankedItem<int>> expected = [new(Rank: 1, Item: 10), new(Rank: 1, Item: 10), new(Rank: 1, Item: 10)];
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void MixedValues_MixedRanks()
    {
        List<int> source = [10, 5, 5, 2];

        var result = source.RankBy(_ => true, Comparer<int>.Default);

        List<RankedItem<int>> expected = [new(Rank: 1, Item: 10), new(Rank: 2, Item: 5), new(Rank: 2, Item: 5), new(Rank: 4, Item: 2)];
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void ContainsUnrankedItems_UnrankedInTheEnd()
    {
        List<int> source = [10, 5, 5, 2];

        var result = source.RankBy(x => x % 2 == 0, Comparer<int>.Default);

        List<RankedItem<int>> expected = [new(Rank: 1, Item: 10), new(Rank: 2, Item: 2), new(Rank: null, Item: 5), new(Rank: null, Item: 5)];
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void AscendingValues_ThrowsError()
    {
        List<int> source = [10, 5, 5, 7];

        Assert.That(
            Action,
            Throws.InvalidOperationException.With.Message.EqualTo(
                "The source sequence must be sorted in non-ascending order."
            )
        );
        return;

        IEnumerable<RankedItem<int>> Action() => [.. source.RankBy(_ => true, Comparer<int>.Default)];
    }

    [TestCase(1)]
    [TestCase(2)]
    public void AllItemsUnranked_AllRanksAreNull(int count)
    {
        var source = Enumerable.Range(1, count).Reverse().ToArray();

        var result = source.RankBy(_ => false, Comparer<int>.Default);

        Assert.That(result, Is.EqualTo(source.Select(item => new RankedItem<int>(null, item))));
    }

    [Test]
    public void FirstItemsUnranked_RankingStartsAtFirstEligibleItem()
    {
        int[] source = [11, 9, 8, 8, 7, 4];

        var result = source.RankBy(item => item % 2 == 0, Comparer<int>.Default);

        RankedItem<int>[] expected = [new(1, 8), new(1, 8), new(3, 4), new(null, 11), new(null, 9), new(null, 7)];
        Assert.That(result, Is.EqualTo(expected));
    }
}
