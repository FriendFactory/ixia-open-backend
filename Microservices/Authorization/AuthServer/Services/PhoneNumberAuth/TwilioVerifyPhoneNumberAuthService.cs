using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuthServer.Services.SmsSender;
using Common.Infrastructure;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Twilio.Exceptions;
using Twilio.Rest.Lookups.V1;
using Twilio.Rest.Verify.V2.Service;
using Twilio.Types;

namespace AuthServer.Services.PhoneNumberAuth;

public class TwilioVerifyPhoneNumberAuthService : IPhoneNumberAuthService
{
    private static readonly int[] TwilioExceptionCodes = [60202, 60203, 60212, 60308, 60410];
    private const string NextRetryDateFormat = "yyyy-MM-ddTHH:mm:ss";
    private static readonly Regex NormalizePhoneRegex = new(@"[^\d]");
    private static readonly Regex ExternalCodeRegex = new(@"^\d{6}$");
    private static readonly int[] MessageDelays = [60, 90, 120, 300];
    private readonly ILogger _log;
    private readonly IDatabase _redis;

    private readonly TwilioSmsSettings _twilioSettings;
    private readonly IValidator<VerifyPhoneNumberRequest> _validator;

    public TwilioVerifyPhoneNumberAuthService(
        TwilioSmsSettings twilioSettings,
        IConnectionMultiplexer redisConnection,
        ILoggerFactory loggerFactory,
        IValidator<VerifyPhoneNumberRequest> validator
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(redisConnection);

        _twilioSettings = twilioSettings ?? throw new ArgumentNullException(nameof(twilioSettings));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _redis = redisConnection.GetDatabase();
        _log = loggerFactory.CreateLogger("Frever.TwilioPhoneNumberAuthService");
    }

    public async Task<VerifyPhoneNumberResponse> SendPhoneNumberVerification(VerifyPhoneNumberRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _validator.ValidateAndThrowAsync(request);

        NextRetryInfo next = null;

        var normalizedPhoneNumber = NormalizePhoneRegex.Replace(request.PhoneNumber, string.Empty);
        var key = SmsSendingRateLimitCacheKey(normalizedPhoneNumber);
        var nextRetryInfoString = await _redis.StringGetAsync(key);

        if (!string.IsNullOrWhiteSpace(nextRetryInfoString))
            next = ParseNextRetryString(nextRetryInfoString);

        var now = DateTime.UtcNow;

        // Rate limit not yet passed
        if (next != null && next.NextRetryAfter > now)
        {
            var nextResendIn = next.NextRetryAfter - now;
            return new VerifyPhoneNumberResponse
                   {
                       IsSuccessful = false,
                       SecondsTillNextRetry = (int) nextResendIn.TotalSeconds,
                       ErrorCode = ErrorCodes.Auth.PhoneNumberRetryNotAvailable,
                       ErrorMessage = $"New code can be send in {nextResendIn}"
                   };
        }

        var nextDelaySeconds = MessageDelays.FirstOrDefault(d => d > (next?.Seconds ?? 0));
        if (nextDelaySeconds == 0)
            nextDelaySeconds = MessageDelays.Max();

        next = new NextRetryInfo {Seconds = nextDelaySeconds, NextRetryAfter = now + TimeSpan.FromSeconds(nextDelaySeconds)};

        try
        {
            var verification = await VerificationResource.CreateAsync(
                                   to: request.PhoneNumber,
                                   channel: "sms",
                                   pathServiceSid: _twilioSettings.VerifyServiceSid
                               );

            nextRetryInfoString = FormatNextRetryInfo(next.Seconds, next.NextRetryAfter);
            await _redis.StringSetAsync(key, nextRetryInfoString, TimeSpan.FromMinutes(5));

            if (verification.Status == "pending")
                return new VerifyPhoneNumberResponse {IsSuccessful = true, SecondsTillNextRetry = next.Seconds};

            return new VerifyPhoneNumberResponse {IsSuccessful = false, SecondsTillNextRetry = next.Seconds};
        }
        catch (ApiException ex)
        {
            _log.LogError(ex, "Error sending verification code to phone number with Twilio, Error code {ExCode}", ex.Code);

            nextRetryInfoString = FormatNextRetryInfo(next.Seconds, next.NextRetryAfter);
            await _redis.StringSetAsync(key, nextRetryInfoString, TimeSpan.FromMinutes(5));

            if (TwilioExceptionCodes.Contains(ex.Code))
                return new VerifyPhoneNumberResponse
                       {
                           IsSuccessful = false,
                           ErrorCode = ErrorCodes.Auth.PhoneNumberTooManyRequests,
                           ErrorMessage =
                               "Oops! It looks like there have been too many sign-ups from this device with phone numbers recently. " +
                               "Try again later or sign up with email instead."
                       };

            throw AppErrorWithStatusCodeException.BadRequest("Error verifying phone number", ErrorCodes.Auth.PhoneNumberVerificationError);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error sending verification code to phone number with Twilio");

            nextRetryInfoString = FormatNextRetryInfo(next.Seconds, next.NextRetryAfter);
            await _redis.StringSetAsync(key, nextRetryInfoString, TimeSpan.FromMinutes(5));

            throw AppErrorWithStatusCodeException.BadRequest("Error verifying phone number", ErrorCodes.Auth.PhoneNumberVerificationError);
        }
    }

    /// <summary>
    ///     Client flow requires immediate generating new code after validating current one.
    ///     It's not possible to do with Twilio Verify service, so we support two types of code:
    ///     - external -- sent to user by Twilio, six digits
    ///     - internal -- guid we generate and store in redis
    /// </summary>
    public async Task<bool> ValidateVerificationCode(string phoneNumber, string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(verificationCode))
            return false;

        if (ExternalCodeRegex.IsMatch(verificationCode))
            try
            {
                var response = await VerificationCheckResource.CreateAsync(
                                   to: phoneNumber,
                                   code: verificationCode,
                                   pathServiceSid: _twilioSettings.VerifyServiceSid
                               );
                return response.Status == "approved";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error validate verification code with Twilio");
                throw AppErrorWithStatusCodeException.BadRequest("Error checking verification code", "ErrorCheckingVerificationCode");
            }

        var key = InternalCodeCacheKey(phoneNumber);
        string val = await _redis.StringGetAsync(key);
        await _redis.KeyDeleteAsync(key);
        return val == verificationCode;
    }

    public async Task<string> GenerateVerificationCode(string phoneNumber)
    {
        var key = InternalCodeCacheKey(phoneNumber);
        var code = Guid.NewGuid().ToString("N");

        await _redis.StringSetAsync(key, code, TimeSpan.FromMinutes(5));

        return code;
    }

    public async Task<string> FormatPhoneNumber(string phoneNumber)
    {
        try
        {
            var formattedPhoneNumber = await PhoneNumberResource.FetchAsync(new PhoneNumber(phoneNumber));

            return formattedPhoneNumber?.PhoneNumber.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(phoneNumber));

        return NormalizePhoneRegex.Replace(phoneNumber, string.Empty);
    }

    private static string InternalCodeCacheKey(string phoneNumber)
    {
        return $"frever::auth::otp::internal::{NormalizePhoneNumber(phoneNumber)}".FreverVersionedCache();
    }

    private static string SmsSendingRateLimitCacheKey(string phoneNumber)
    {
        return $"frever::rate-limit::sms::otp::{phoneNumber}";
    }

    private static NextRetryInfo ParseNextRetryString(string info)
    {
        if (string.IsNullOrWhiteSpace(info))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(info));

        var parts = info.Split(" ");
        if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Next retry info has incorrect format");

        var seconds = int.Parse(parts[0]);
        var after = DateTime.ParseExact(parts[1], NextRetryDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                            .ToUniversalTime();

        return new NextRetryInfo {Seconds = seconds, NextRetryAfter = after};
    }

    private static string FormatNextRetryInfo(int seconds, DateTime after)
    {
        return $"{seconds} {after.ToString(NextRetryDateFormat, CultureInfo.InvariantCulture)}";
    }

    private class NextRetryInfo
    {
        public int Seconds { get; init; }

        public DateTime NextRetryAfter { get; init; }
    }
}