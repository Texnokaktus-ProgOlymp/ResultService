using Texnokaktus.ProgOlymp.ResultService.Domain;

namespace Texnokaktus.ProgOlymp.ResultService.Extensions;

internal static class RankingExtensions
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        public IEnumerable<RankedItem<TSource>> RankBy(
            Func<TSource, bool> rankingCondition,
            IComparer<TSource> comparer
        )
        {
            var unrankedItems = new List<TSource>();
            var previousPlace = 1;
            var place = 0;
            TSource? previousItem = default;

            foreach (var currentItem in source)
            {
                if (!rankingCondition.Invoke(currentItem))
                {
                    unrankedItems.Add(currentItem);
                    continue;
                }

                place++;

                if (place > 1)
                    previousPlace = comparer.Compare(previousItem, currentItem) switch
                    {
                        > 0 => place,
                        0   => previousPlace,
                        _   => throw new InvalidOperationException(
                                   "The source sequence must be sorted in non-ascending order."
                               )
                    };

                previousItem = currentItem;

                yield return new(previousPlace, currentItem);
            }

            foreach (var item in unrankedItems)
                yield return new(null, item);
        }
    }
}
