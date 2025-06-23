using System;
using System.Threading.Tasks;

namespace Frever.Video.Core.Features.MediaConversion.Mp3Extraction;

public interface IVideoToMp3TranscodingService
{
    Task<TranscodingInfo> InitTranscoding();

    Task<TranscodeResult> Transcode(string transcodingId, TimeSpan duration);
}