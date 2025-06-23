using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure;

public static class LoggerExtension
{
    /// <summary>
    ///     Writes log message when return value is disposed, measuring time elapsed since method call till disposing.
    ///     Elapsed time is prepend to list of <paramref name="args" />.
    /// </summary>
    public static IDisposable LogTime(this ILogger log, Func<TimeSpan, (LogLevel, string)> messageBuilder, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(messageBuilder);

        return new LogTimeMeasurer(log, messageBuilder, args);
    }

    public static IDisposable LogTime(this ILogger log, TimeSpan errorTimeLevel, string message, params object[] args)
    {
        return log.LogTime(elapsed => (elapsed >= errorTimeLevel ? LogLevel.Error : LogLevel.Information, message), args);
    }

    private readonly struct LogTimeMeasurer(ILogger log, Func<TimeSpan, ( LogLevel, string)> messageBuilder, object[] args) : IDisposable
    {
        private readonly Func<TimeSpan, (LogLevel, string)> _messageBuilder = messageBuilder ?? throw new ArgumentNullException(nameof(messageBuilder));
        private readonly ILogger _log = log ?? throw new ArgumentNullException(nameof(log));
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public void Dispose()
        {
            _sw.Stop();
            var (level, message) = _messageBuilder(_sw.Elapsed);

            var a = args == null || args.Length == 0 ? [_sw.Elapsed] : args.Prepend(_sw.Elapsed).ToArray();

            _log.Log(level, message, a);
        }
    }
}