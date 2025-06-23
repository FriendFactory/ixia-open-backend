using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NotificationService.Core;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class MyNotificationsController(INotificationReadService notificationReadService) : ControllerBase
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver()};

    private readonly INotificationReadService _notificationReadService = notificationReadService ?? throw new ArgumentNullException(nameof(notificationReadService));

    static MyNotificationsController()
    {
        JsonSerializerSettings.Converters.Add(new StringEnumConverter());
    }

    [HttpGet("")]
    public async Task<ActionResult> GetMyNotifications([FromQuery(Name = "$skip")] int skip = 0, [FromQuery(Name = "$top")] int top = 50)
    {
        var result = await _notificationReadService.MyNotifications(skip, top);

        if (Request.Headers.Accept.Any(v => v.Contains(ProtobufOutputFormatter.ProtobufMimeType, StringComparison.OrdinalIgnoreCase)))
            return Ok(result);

        var serializedItems = JsonConvert.SerializeObject(result, JsonSerializerSettings);

        return Content(serializedItems, "application/json");
    }

    [HttpPut]
    [Route("mark-as-read")]
    public async Task<ActionResult> MarkAsRead([FromBody] long[] notificationIds)
    {
        if (notificationIds != null && notificationIds.Length != 0)
            await _notificationReadService.MarkNotificationsAsRead(notificationIds);

        return NoContent();
    }
}