using System;
using System.Linq;
using System.Threading.Tasks;
using AssetStoragePathProviding;
using AssetStoragePathProviding.Settings;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

internal class FileOwnerValidator<TEntity>(IAssetFilesConfigs assetFilesConfigs) : IEntityValidator<TEntity>
    where TEntity : class, IFileOwner, IEntity
{
    private readonly IAssetFilesConfigs _assetFilesConfigs = assetFilesConfigs ?? throw new ArgumentNullException(nameof(assetFilesConfigs));

    public Task<ValidationResult> Validate(TEntity model, CallContext context)
    {
        if (context.Operation == WriteOperation.Delete)
            return Task.FromResult(ValidationResult.Valid);

        if (context.Operation == WriteOperation.Create && model.Files == null)
            return Task.FromResult(ValidationResult.Fail("Null instead of files array"));

        if (model.Files != null && model.Files.Any(f => f == null))
            return Task.FromResult(ValidationResult.Fail("Null element in files array"));

        // Don't use context.Operation here to avoid
        // incorrect validation for nested entities
        if (model.Id == 0)
            return Task.FromResult(
                CheckRequiredFileTypes(model)
                   .Chain(() => CheckSupportedFileTypes(model))
                   .Chain(() => ValidateExtension(model))
                   .Chain(() => FileSourceValidation(model))
            );

        // Don't use context.Operation here to avoid
        // incorrect validation for nested entities
        if (model.Id != 0)
        {
            if (model.Files != null && model.Files.Any())
                return Task.FromResult(CheckSupportedFileTypes(model).Chain(() => ValidateExtension(model)));

            return Task.FromResult(ValidationResult.Valid);
        }

        return Task.FromResult(ValidationResult.Valid);
    }

    protected virtual ValidationResult CheckRequiredFileTypes(TEntity model)
    {
        var requiredSettings = _assetFilesConfigs.GetSettings(model.GetType());

        if (IsRequiredFileMissed(model, requiredSettings))
            return ValidationResult.Fail(requiredSettings.Select(e => $"FileType:{e} is required for model {model.GetType()}"));

        if (model.Files == null || !model.Files.Any())
            return ValidationResult.Valid;

        if (requiredSettings == null)
            return ValidationResult.Fail($"{model.GetType().Name} does not have any required files");

        foreach (var requiredSetting in requiredSettings)
        {
            var relevantFile = model.Files.FirstOrDefault(x => requiredSetting.IsValidFor(x));

            if (relevantFile == null)
                return ValidationResult.Fail($"Missed file for {model.GetType().Name} {requiredSetting}");
        }

        return ValidationResult.Valid;
    }

    protected virtual bool IsRequiredFileMissed(TEntity model, FileSettings[] requiredSettings)
    {
        return requiredSettings?.Length > 0 && model.Files?.Count == 0;
    }

    protected virtual ValidationResult CheckSupportedFileTypes(TEntity model)
    {
        if (!(model.Files?.Any() ?? false))
            return ValidationResult.Valid; // nothing to validate

        var supportedFileTypes = _assetFilesConfigs.GetSettings(model.GetType());
        var notSupportedFileTypes = model.Files.Where(x => supportedFileTypes.All(_ => !_.IsValidFor(x))).ToArray();

        return ValidationResult.Fail(
            notSupportedFileTypes.Select(fileInfo => $"Not valid file info:{fileInfo} for model {model.GetType()}")
        );
    }

    protected virtual ValidationResult ValidateExtension(TEntity model)
    {
        if (!(model.Files?.Any() ?? false))
            return ValidationResult.Valid; // nothing to validate

        var result = ValidationResult.Valid;

        foreach (var fileInfo in model.Files)
        {
            var expectedEnum = _assetFilesConfigs.GetExtensions(model.GetType(), fileInfo.File);

            if (expectedEnum.Contains(fileInfo.Extension))
                continue;

            var message =
                $"Not supported file extension. Model {model.GetType().Name} {fileInfo.File} expect {string.Join(',', expectedEnum)} extension, but it has extension:'{fileInfo.Extension}' .";
            result = result.WithErrors(message);
        }

        return result;
    }

    protected virtual ValidationResult FileSourceValidation(TEntity model)
    {
        var result = ValidationResult.Valid;

        if (!(model.Files?.Any() ?? false))
            return result; // nothing to validate

        foreach (var fileInfo in model.Files)
            if (string.IsNullOrEmpty(fileInfo.Source?.UploadId) && fileInfo.Source?.CopyFrom == null)
            {
                var message =
                    $"Model {model.GetType().Name} {nameof(model.Files)}:{fileInfo.File} property {nameof(fileInfo.Source)} is empty or not properly defined";
                result = result.WithErrors(message);
            }
            else if (fileInfo.Source.CopyFrom != null && fileInfo.Source.CopyFrom.Id <= 0)
            {
                var message = @$"Model {model.GetType().Name} {nameof(model.Files)}:{fileInfo.File}
property {nameof(fileInfo.Source.CopyFrom)}.{nameof(fileInfo.Source.CopyFrom.Id)}
must be grater then 0.";

                result = result.WithErrors(message);
            }

        return result;
    }
}