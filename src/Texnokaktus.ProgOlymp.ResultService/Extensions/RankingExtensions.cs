using Texnokaktus.ProgOlymp.ResultService.Domain;

namespace Texnokaktus.ProgOlymp.ResultService.Extensions;

public static class RankingExtensions
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        public IEnumerable<RankedItem<TSource>> RankBy(
            Func<TSource, bool> rankingCondition,
            IComparer<TSource> comparer
        )
        {
            using var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext()) yield break;

            var previousPlace = 1;
            var place = 1;
            var previousItem = enumerator.Current;

            yield return new(previousPlace, enumerator.Current);

            var unrankedItems = new List<TSource>();

            while (enumerator.MoveNext())
            {
                if (!rankingCondition.Invoke(enumerator.Current))
                {
                    unrankedItems.Add(enumerator.Current);
                    continue;
                }

                var currentItem = enumerator.Current;

                place++;

                previousPlace = comparer.Compare(previousItem, currentItem) switch
                {
                    > 0 => place,
                    0   => previousPlace,
                    _   => throw new InvalidOperationException(
                               "The source sequence must be sorted in non-ascending order."
                           )
                };

                previousItem = currentItem;

                yield return new(previousPlace, enumerator.Current);
            }

            foreach (var item in unrankedItems)
                yield return new(null, item);
        }
    }
}
