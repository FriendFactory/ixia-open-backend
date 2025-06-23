using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Infrastructure.Utils;

public static class EnumerableUtils
{
    public static IEnumerable<T> AsSequence<T>(this T element)
    {
        yield return element;
    }

    public static IEnumerable<T> AsSequenceOrEmpty<T>(this T element)
    {
        if (element != null)
            yield return element;
    }

    public static IEnumerable<T> NullToEmpty<T>(this IEnumerable<T> source)
    {
        return source ?? Enumerable.Empty<T>();
    }

    public static int? MaxOrDefault<T>(this IEnumerable<T> source, Func<T, int> getValue, int? defaultValue = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (getValue == null)
            throw new ArgumentNullException(nameof(getValue));

        if (source.Any())
            return source.Max(getValue);

        return defaultValue;
    }

    public static int? MinOrDefault<T>(this IEnumerable<T> source, Func<T, int> getValue, int? defaultValue = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (getValue == null)
            throw new ArgumentNullException(nameof(getValue));

        if (source.Any())
            return source.Min(getValue);

        return defaultValue;
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> processElement)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (processElement == null)
            throw new ArgumentNullException(nameof(processElement));
        var i = 0;

        foreach (var item in source)
        {
            processElement(item, i);
            i++;
        }
    }
}