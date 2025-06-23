using System;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V1;
using Twilio.Types;

namespace AuthServer.Services.SmsSender;

public class TwilioSmsSender(TwilioSmsSettings settings) : ISmsSender
{
    private readonly TwilioSmsSettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));

    public Task SendMessage(string phoneNumber, string text)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(phoneNumber));
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(text));

        MessageResource.Create(new PhoneNumber(phoneNumber), messagingServiceSid: _settings.MessagingServiceSid, body: text);

        return Task.CompletedTask;
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
}