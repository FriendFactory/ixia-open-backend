using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.Views.ViewsExport;

public class AwsS3VideoViewsExportService : IVideoViewsExportService
{
    private static readonly string ExportFolder = "video-views/export/";
    private static readonly string ExportFileExtension = ".csv";
    private static readonly string DateFormat = "yyyy-MM-ddTHH:mm:ss";
    private readonly IWriteDb _db;
    private readonly ILogger _log;

    private readonly AwsS3ViewsExporterOptions _options;
    private readonly IAmazonS3 _s3;

    public AwsS3VideoViewsExportService(AwsS3ViewsExporterOptions options, IAmazonS3 s3, IWriteDb db, ILoggerFactory logFactory)
    {
        ArgumentNullException.ThrowIfNull(logFactory);

        _options = options ?? throw new ArgumentNullException(nameof(options));
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _options.Validate();

        _log = logFactory.CreateLogger("Frever.Video.ViewsExporter");
    }

    public async Task DoExport()
    {
        _log.LogTrace("Begin exporting video views to s3://{b}{path}", _options.S3Bucket, ExportFolder);

        var (lastExportDate, exportFileName) = await GetExportInfo();

        _log.LogInformation("Begin exporting views since {b} to s3://{path}", lastExportDate.ToLongDateString(), exportFileName);

        _log.LogTrace("Getting video views since {0}", lastExportDate.ToString(DateFormat, CultureInfo.InvariantCulture));
        var views = await GetViews(lastExportDate.ToUniversalTime());

        if (views.Length == 0)
        {
            _log.LogInformation("No new views since last export");
            return;
        }

        _log.LogTrace("{0} views retrieved", views.Length);

        var csv = ToCsv(views);

        _log.LogTrace("Writing CSV to s3://{1}{0}", _options.S3Bucket, exportFileName);
        await WriteCsvToS3(exportFileName, csv);

        _log.LogInformation(
            "Exported {count} views (since {date}) to s3://{bucket}{key}",
            views.Length,
            lastExportDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            _options.S3Bucket,
            exportFileName
        );
    }

    private StringBuilder ToCsv(VideoView[] views)
    {
        var builder = new StringBuilder();

        foreach (var item in views)
            builder.AppendFormat("{0} {1} {2}", item.UserId, item.Time.ToString(DateFormat, CultureInfo.InvariantCulture), item.VideoId)
                   .AppendLine();

        return builder;
    }

    private Task<VideoView[]> GetViews(DateTime sinceDate)
    {
        return _db.VideoView.Where(v => v.Time >= sinceDate).ToArrayAsync();
    }

    private async Task<(DateTime lastExportDate, string exportFilePath)> GetExportInfo()
    {
        var lastExportDate = DateTime.UtcNow.AddDays(-7); // For empty bucket export views for 7 days
        var counter = 1000000;                            // Initial counter value. Would be decremented on each export

        var lastExportFileName = await GetLastExportFileName();
        if (!string.IsNullOrWhiteSpace(lastExportFileName))
        {
            var (d, c) = ParseExportFileName(lastExportFileName);
            lastExportDate = d;
            counter = c - 1;
        }

        var exportDate = DateTime.UtcNow;
        var newExportFileName = FormatExportFileName(exportDate, counter);

        return (lastExportDate, newExportFileName);
    }

    private async Task WriteCsvToS3(string fileKey, StringBuilder csv)
    {
        await _s3.PutObjectAsync(new PutObjectRequest {BucketName = _options.S3Bucket, Key = fileKey, ContentBody = csv.ToString()});
    }

    /// <summary>
    ///     Gets the name of the last exported file on bucket.
    /// </summary>
    private async Task<string> GetLastExportFileName()
    {
        var result = await _s3.ListObjectsV2Async(
                         new ListObjectsV2Request {BucketName = _options.S3Bucket, Prefix = ExportFolder, MaxKeys = 1}
                     );

        if (result.KeyCount > 0)
            return result.S3Objects[0].Key;

        return string.Empty;
    }

    /// <summary>
    ///     Parse export file name.
    ///     File name format is [counter]_[date_time_of_export].csv
    ///     Each export counter should be decremented (and padded with zeros to 6 chars).
    ///     That is needed to put latest export file on top of file list to easily get it
    ///     with ListObjectV2 request.
    /// </summary>
    private static (DateTime dateTime, int counter) ParseExportFileName(string exportFileName)
    {
        if (string.IsNullOrWhiteSpace(exportFileName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exportFileName));

        if (exportFileName.StartsWith(ExportFolder))
            exportFileName = exportFileName.Substring(ExportFolder.Length);

        if (exportFileName.EndsWith(ExportFileExtension))
            exportFileName = exportFileName.Replace(ExportFileExtension, string.Empty);

        var parts = exportFileName.Split("_");
        if (parts.Length != 2)
            throw new ArgumentException("Export file name has invalid format", nameof(exportFileName));

        var counterStr = parts[0];
        var dateTimeStr = parts[1];

        var counter = int.Parse(counterStr);
        var dateTime = DateTime.ParseExact(dateTimeStr, DateFormat, CultureInfo.InvariantCulture);

        return (dateTime, counter);
    }

    private static string FormatExportFileName(DateTime exportDate, int counter)
    {
        return
            $"{ExportFolder}{counter.ToString().PadLeft(7, '0')}_{exportDate.ToString(DateFormat, CultureInfo.InvariantCulture)}{ExportFileExtension}";
    }
}