using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.Aws;

public interface IAwsRetryHelper
{
    /// <summary>
    ///     Runs specified AWS operation certain amount of times.
    ///     If operation throws TooManyRequests exception then wait for specified amount of milliseconds and try again.
    /// </summary>
    /// <param name="awsCall">AWS API call to retry.</param>
    /// <param name="logger">Optional custom logger to write diagnostics to.</param>
    /// <param name="retryCount">Number of retries, default is 20</param>
    /// <param name="delayBetweenRetryMilliseconds">
    ///     Desired delay between retries in milliseconds.
    ///     Actual delay is random in range of +- 150 milliseconds of desired delay.
    ///     That's done to better distribution of calls.
    /// </param>
    /// <typeparam name="TResult">Type of result.</typeparam>
    /// <returns>A task resolves to result first successful AWS call.</returns>
    Task<TResult> FightAwsThrottling<TResult>(
        Expression<Func<Task<TResult>>> awsCall,
        ILogger logger = null,
        int retryCount = 20,
        int delayBetweenRetryMilliseconds = 300
    );
}

public class AwsRetryHelper(ILoggerFactory loggerFactory) : IAwsRetryHelper
{
    private static readonly Random Rnd = new();
    private readonly ILogger _defaultLogger = loggerFactory.CreateLogger("Frever.AwsRetryHelper");

    public async Task<TResult> FightAwsThrottling<TResult>(
        Expression<Func<Task<TResult>>> awsCall,
        ILogger logger = null,
        int retryCount = 20,
        int delayBetweenRetryMilliseconds = 300
    )
    {
        logger ??= _defaultLogger;
        var compiledAwsCall = awsCall.Compile();
        var functionName = GetFunctionName(awsCall);

        for (var retry = 1; retry <= retryCount; retry++)
            try
            {
                return await compiledAwsCall();
            }
            catch (Exception ex)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals("TooManyRequestsException", ex.GetType().Name))
                {
                    logger.LogInformation("AWS API call of {FunctionName} throttled {Retry} times", functionName, retry);

                    var actualDelay = Rnd.Next(Math.Min(100, delayBetweenRetryMilliseconds - 150), delayBetweenRetryMilliseconds + 150);

                    await Task.Delay(actualDelay);

                    continue;
                }

                throw;
            }

        var error = $"AWS API call of {functionName} exceeded maximum retries {retryCount}";
        logger.LogError(error);

        throw new AwsThrottlingException(error);
    }

    private string GetFunctionName<TResult>(Expression<Func<TResult>> expression)
    {
        if (expression is LambdaExpression lambda)
            if (lambda.Body is MethodCallExpression methodCall)
                return methodCall.Method.Name;

        throw new ArgumentException("Expression is not a method call", nameof(expression));
    }
}

public class AwsThrottlingException(string message) : Exception(message);