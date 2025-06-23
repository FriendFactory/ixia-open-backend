using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Infrastructure.Utils;

public static class RndEnumerableUtils
{
    public static TElement RandomElement<TElement>(this IEnumerable<TElement> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(s => new {Element = s, Key = Guid.NewGuid()}).OrderBy(e => e.Key).Select(e => e.Element).FirstOrDefault();
    }

    /// <summary>
    ///     Picks the random elements not picked before
    /// </summary>
    public static TElement RandomElement<TElement>(this IEnumerable<TElement> source, ISet<TElement> pickedElements)
    {
        var result = source.Where(s => !pickedElements.Contains(s)).RandomElement();
        if (!ReferenceEquals(result, null))
            pickedElements.Add(result);

        return result;
    }

    /// <summary>
    ///     Selects a random element from sequence taking weight into account.
    ///     The probability of selecting certain element is the greater the greater the weight.
    ///     Negative or zero weighted elements is excluded unless all elements are negative or zero.
    /// </summary>
    public static TResult RandomWeighted<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal> weightSelector,
        Func<TSource, TResult> resultSelector
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(weightSelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        if (!source.Any())
            throw new InvalidOperationException("Source sequence is empty");

        var src = source.Select(r => new {Element = r, Weight = Math.Max(0, weightSelector(r))}).ToArray();

        var totalWeight = src.Sum(a => a.Weight);

        var src2 = (totalWeight == 0
                        ? src.Select(a => new {a.Element, Weight = 1.0m / src.Length})
                        : src.Select(a => new {a.Element, Weight = a.Weight / totalWeight})).ToArray();

        var rnd = (decimal) new Random().NextDouble();

        var sum = 0.0m;

        foreach (var item in src2)
        {
            if (sum <= rnd && sum + item.Weight > rnd)
                return resultSelector(item.Element);
            sum += item.Weight;
        }

        return resultSelector(src2[0].Element);
    }

    public static IEnumerable<T> Randomize<T>(this IEnumerable<T> items)
    {
        return items.Select(i => new {Item = i, Index = Guid.NewGuid()}).OrderBy(v => v.Index).Select(v => v.Item);
    }
}