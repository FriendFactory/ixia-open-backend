using System;
using System.Collections.Generic;

namespace Common.Infrastructure.Utils;

public static class DateUtils
{
    private static readonly Dictionary<DayOfWeek, int> StartOfWeekAdjustments = new()
                                                                                {
                                                                                    {DayOfWeek.Monday, 0},
                                                                                    {DayOfWeek.Tuesday, -1},
                                                                                    {DayOfWeek.Wednesday, -3},
                                                                                    {DayOfWeek.Thursday, -4},
                                                                                    {DayOfWeek.Friday, -5},
                                                                                    {DayOfWeek.Saturday, -6},
                                                                                    {DayOfWeek.Sunday, -7}
                                                                                };

    public static DateTime StartOfDay(this DateTime date)
    {
        return new DateTime(
            date.Year,
            date.Month,
            date.Day,
            0,
            0,
            0,
            DateTimeKind.Utc
        );
    }

    public static DateTime EndOfDay(this DateTime date)
    {
        return date.StartOfDay().AddDays(1).AddMilliseconds(-1);
    }

    public static DateTime StartOfWeek(this DateTime date)
    {
        return date.AddDays(StartOfWeekAdjustments[date.DayOfWeek]).StartOfDay();
    }

    public static TimeSpan TillTheEndOfTheDay(this DateTime date)
    {
        return date.EndOfDay() - date;
    }

    /// <summary>
    ///     Gets DateTime indicates start of period when consequent activities is about to be capped.
    /// </summary>
    /// <remarks>
    ///     Capping period of 1 day means cappings are calculated due current day (that's why we deduct 1 from capping days
    ///     value).
    /// </remarks>
    public static DateTime ToCappingPeriodStart(this DateTime value, int cappingDays)
    {
        return value.AddDays(-(Math.Abs(cappingDays) - 1)).StartOfDay().ToLocalTime();
    }

    /// <summary>
    ///     Changes DateTime.Kind to Universal with keeping current time.
    /// </summary>
    public static DateTime AsUniversal(this DateTime value)
    {
        return value.AsKind(DateTimeKind.Utc);
    }

    /// <summary>
    ///     Changes DateTime.Kind to given value with keeping current time.
    /// </summary>
    public static DateTime AsKind(this DateTime value, DateTimeKind kind)
    {
        return new DateTime(
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            value.Second,
            value.Millisecond,
            value.Microsecond,
            kind
        );
    }

    public static DateTime FixKindToUniversal(this DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();

        return value.AsUniversal();
    }

    public static DateTime FixKindToLocal(this DateTime value)
    {
        if (value.Kind == DateTimeKind.Local)
            return value;
        if (value.Kind == DateTimeKind.Utc)
            return value.ToLocalTime();

        return value.AsKind(DateTimeKind.Local);
    }
}