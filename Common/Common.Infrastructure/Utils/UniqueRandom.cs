using System;
using System.Collections.Generic;

namespace Common.Infrastructure.Utils;

/// <summary>
///     Wrapper over random number generator which try to generate non-repeatable random numbers
/// </summary>
public class UniqueRandom
{
    private const int MaxTryouts = 10;
    private readonly HashSet<int> _generated = [];

    private readonly Random _random = new();


    public int Next(int min, int max)
    {
        var next = _random.Next(min, max);

        for (var i = 0; i < MaxTryouts; i++)
        {
            if (_generated.Add(next))
                return next;

            next = _random.Next(min, max);
        }

        return next;
    }
}