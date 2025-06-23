using System;
using AspNetCoreRateLimit;
using Common.Infrastructure.EmailSending;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Infrastructure.RateLimit;

public class AlertingClientRateLimitMiddleware(
    RequestDelegate next,
    IProcessingStrategy processingStrategy,
    IOptions<ClientRateLimitOptions> options,
    IClientPolicyStore policyStore,
    IRateLimitConfiguration config,
    IEmailSendingService emailSendingService,
    EnvironmentInfo.EnvironmentInfo environmentInfo,
    ILogger<ClientRateLimitMiddleware> logger
) : ClientRateLimitMiddleware(
    next,
    processingStrategy,
    options,
    policyStore,
    config,
    logger
)
{
    public static readonly string[] EmailAddr = [];

    private readonly IEmailSendingService _emailSendingService =
        emailSendingService ?? throw new ArgumentNullException(nameof(emailSendingService));

    private readonly EnvironmentInfo.EnvironmentInfo _environmentInfo = environmentInfo ?? throw new ArgumentNullException(nameof(environmentInfo));

    protected override void LogBlockedRequest(
        HttpContext httpContext,
        ClientRequestIdentity identity,
        RateLimitCounter counter,
        RateLimitRule rule
    )
    {
        base.LogBlockedRequest(httpContext, identity, counter, rule);

        if (httpContext?.User == null)
            return;

        var groupId = httpContext.User.FindFirst("PrimaryGroupId")?.Value;
        var userId = httpContext.User.FindFirst("UserId")?.Value;

        if (groupId == null || userId == null)
            return;

        var emailParams = new SendEmailParams
                          {
                              To = EmailAddr,
                              Subject = $"User {userId} with GroupId {groupId} has exceeded rate limit in Env {_environmentInfo.Type}",
                              Body =
                                  @$"User {userId} with GroupId {groupId} has exceeded rate limit at {DateTime.Now} in Env {_environmentInfo.Type}.
                                      Request {identity.HttpVerb}:{identity.Path} from ClientId {identity.ClientId} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.Count - rule.Limit}.
                                      Blocked by rule {rule.Endpoint}, TraceIdentifier {httpContext.TraceIdentifier}. MonitorMode: {rule.MonitorMode}. "
                          };
        _emailSendingService.SendEmail(emailParams);
    }
}