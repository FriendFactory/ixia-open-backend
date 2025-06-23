using System;
using System.Threading.Tasks;

namespace Frever.Client.Core.Features.MediaFingerprinting;

public interface IMediaFingerprintingService
{
    Task<MediaFingerprintingResult> CheckS3File( string key, TimeSpan duration);
}