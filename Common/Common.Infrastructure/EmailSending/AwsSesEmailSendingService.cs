using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;

namespace Common.Infrastructure.EmailSending;

public class AwsSesEmailSendingService : IEmailSendingService
{
    private readonly EmailConfiguration _emailConfiguration;
    private readonly ILogger _log;
    private readonly IAmazonSimpleEmailService _ses;

    public AwsSesEmailSendingService(ILoggerFactory loggerFactory, EmailConfiguration emailConfiguration, IAmazonSimpleEmailService ses)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _emailConfiguration = emailConfiguration ?? throw new ArgumentNullException(nameof(emailConfiguration));
        _ses = ses ?? throw new ArgumentNullException(nameof(ses));
        _log = loggerFactory.CreateLogger("EmailSendingService");
    }

    public async Task SendEmail(SendEmailParams parameters)
    {
        _log.LogInformation("Email send to {ParametersTo} with subject {ParametersSubject}", parameters.To, parameters.Subject);

        var response = await _ses.SendEmailAsync(
                           new SendEmailRequest
                           {
                               Destination = new Destination {ToAddresses = parameters.To.ToList(), CcAddresses = parameters.Cc?.ToList()},
                               Message = new Message
                                         {
                                             Body = new Body(new Content(parameters.Body)), Subject = new Content(parameters.Subject)
                                         },
                               Source = _emailConfiguration.FromEmail
                           }
                       );

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            _log.LogError(
                "Error sending email to {ParametersTo}. Message ID {ResponseMessageId}, status {ResponseHttpStatusCode}",
                parameters.To,
                response.MessageId,
                response.HttpStatusCode
            );

            throw new InvalidOperationException(
                $"Error sending email to {parameters.To}. Message ID {response.MessageId}, status {response.HttpStatusCode}"
            );
        }

        _log.LogDebug("Message {ResponseMessageId} sent", response.MessageId);
    }
}