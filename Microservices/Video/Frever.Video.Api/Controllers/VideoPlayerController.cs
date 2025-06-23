using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS8604

namespace Frever.Video.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("video")]
public class VideoPlayerController : ControllerBase
{
    [HttpGet]
    [Route("play")]
    public async Task<ActionResult> VideoViewPage()
    {
        return Content(await LoadVideoPlayerHtml(), "text/html");
    }

    private static async Task<string> LoadVideoPlayerHtml()
    {
        var asmName = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
        var dir = Path.GetDirectoryName(asmName);

        var html = Path.Combine(dir, "view-video.html");

        var content = await System.IO.File.ReadAllTextAsync(html);

        return content;
    }
}