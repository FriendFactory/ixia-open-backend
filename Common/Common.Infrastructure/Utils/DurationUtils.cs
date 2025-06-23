using System;

namespace Common.Infrastructure.Utils;

public static class DurationUtils
{
    private static readonly TimeSpan MinSpreadableTimespan = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     Adds random duration less than <paramref name="dispersion" /> to initial value,
    ///     if value is less than 10 min.
    /// </summary>
    public static TimeSpan Spread(this TimeSpan value, TimeSpan dispersion)
    {
        if (value == TimeSpan.MaxValue)
            return value;

        if (value >= MinSpreadableTimespan)
        {
            if (dispersion > TimeSpan.FromMinutes(120))
                dispersion = TimeSpan.FromMinutes(120);

            var rnd = new Random(); // For thread safety
            var spread = rnd.NextDouble();
            var randomSpread = TimeSpan.FromSeconds(dispersion.TotalSeconds * spread);

            return value + randomSpread;
        }

        return value;
    }

    public static TimeSpan? Spread(this TimeSpan? value, TimeSpan dispersion)
    {
        if (value == null)
            return value;

        return value.Value.Spread(dispersion);
    }

    /// <summary>
    ///     Adds random duration equal to 10% of <paramref name="value" />.
    /// </summary>
    public static TimeSpan Spread(this TimeSpan value)
    {
        var dispersion = TimeSpan.FromSeconds(value.TotalSeconds * 0.1);
        return value.Spread(dispersion);
    }

    public static TimeSpan? Spread(this TimeSpan? value)
    {
        if (value == null)
            return value;

        return value.Value.Spread();
    }
}