using System;
using System.Collections.Generic;
using System.Linq;
using AssetStoragePathProviding;
using AssetStoragePathProviding.Settings;
using Common.Infrastructure;
using Common.Models.Files;
using FluentValidation;

namespace Frever.AdminService.Core.Validation;

public static class ValidationExtensions
{
    public static void EntityRef<T>(this IRuleBuilderInitial<T, long> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        rule.GreaterThan(0);
    }

    public static IRuleBuilderOptions<T, IEnumerable<FileInfo>> EntityFiles<T>(
        this IRuleBuilderInitial<T, List<FileInfo>> rule,
        IAssetFilesConfigs fileConfig,
        Type entityType
    )
    {
        ArgumentNullException.ThrowIfNull(fileConfig);
        ArgumentNullException.ThrowIfNull(rule);

        return rule.Must(
                        (_, value, context) =>
                        {
                            var entityFileConfig = fileConfig.GetSettings(entityType);

                            foreach (var conf in entityFileConfig)
                            {
                                var existingFile = value.Find(f => conf.IsValidFor(f));
                                if (existingFile == null)
                                {
                                    context.MessageFormatter.AppendArgument(
                                        "error",
                                        $"{conf.FileType} with resolution {conf.Resolution} is missing"
                                    );
                                    return false;
                                }

                                if (conf.Extensions.Contains(existingFile.Extension))
                                    continue;

                                context.MessageFormatter.AppendArgument(
                                    "error",
                                    $"{conf.FileType} with resolution {conf.Resolution} " +
                                    $"has invalid extension: {existingFile.Extension}"
                                );

                                return false;
                            }

                            foreach (var src in value.Where(src => !entityFileConfig.Any(fc => fc.IsValidFor(src))))
                            {
                                context.MessageFormatter.AppendArgument(
                                    "error",
                                    $"Extra file {src.File} with resolution {src.Resolution} is added to files"
                                );
                                return false;
                            }

                            return true;
                        }
                    )
                   .WithMessage($"File error for {typeof(T).Name}: {{error}} ")
                   .ForEach(b => b.SetValidator(new FileInfoValidator()));
    }

    public static void ValidateFiles(List<FileInfo> value, FileSettings[] entityFileConfig)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(entityFileConfig);

        foreach (var fileConf in entityFileConfig)
        {
            var existingFile = value.Find(f => fileConf.IsValidFor(f));
            if (existingFile == null)
                throw AppErrorWithStatusCodeException.BadRequest(
                    $"{fileConf.FileType} with resolution {fileConf.Resolution} is missing",
                    "MissingFile"
                );

            if (!fileConf.Extensions.Contains(existingFile.Extension))
                throw AppErrorWithStatusCodeException.BadRequest(
                    $"{fileConf.FileType} with resolution {fileConf.Resolution} " + $"has invalid extension: {existingFile.Extension}",
                    "InvalidExtension"
                );
        }

        foreach (var src in value.Where(src => !entityFileConfig.Any(fc => fc.IsValidFor(src))))
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Extra file {src.File} with resolution {src.Resolution} is added to files",
                "ExtraFile"
            );
    }
}

public class FileInfoValidator : AbstractValidator<FileInfo>
{
    public FileInfoValidator()
    {
        RuleFor(e => e.File).Must(v => Enum.IsDefined(typeof(FileType), v)).WithMessage("Invalid value for file type");
        RuleFor(e => e.Extension).Must(v => Enum.IsDefined(typeof(FileExtension), v)).WithMessage("Invalid value for file extension");
        RuleFor(e => e.Platform)
           .Must(v => v == null || Enum.IsDefined(typeof(Platform), v.Value))
           .WithMessage("Invalid value for platform");
        RuleFor(e => e.Resolution)
           .Must(v => v == null || Enum.IsDefined(typeof(Resolution), v.Value))
           .WithMessage("Invalid value for resolution");
        RuleFor(e => e.Source).SetValidator(new FileSourceValidator()).When(e => string.IsNullOrWhiteSpace(e.Version));
    }
}

public class FileSourceValidator : AbstractValidator<FileSource>
{
    public FileSourceValidator()
    {
        RuleFor(e => e.UploadId)
           .Must(v => !string.IsNullOrWhiteSpace(v))
           .When(e => e.CopyFrom == null)
           .WithMessage("Upload ID must be provided if copy from is not defined");

        RuleFor(e => e.CopyFrom)
           .NotNull()
           .When(e => string.IsNullOrWhiteSpace(e.UploadId))
           .SetValidator(new CopyFromValidator())
           .When(e => string.IsNullOrWhiteSpace(e.UploadId));
    }
}

public class CopyFromValidator : AbstractValidator<AssetFileSourceInfo>
{
    public CopyFromValidator()
    {
        RuleFor(e => e.Id).EntityRef();
    }
}