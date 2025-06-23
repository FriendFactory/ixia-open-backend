using System;
using Common.Models.Files;
using FluentValidation;

namespace Frever.Client.Shared.Files;

public static class FileMetadataValidationExtension
{
    public static void AddFileMetadataValidation<TModel, TEntity>(
        this AbstractValidator<TModel> validator,
        IAdvancedFileStorageService fileStorageService
    )
        where TModel : class, IFileMetadataOwner
        where TEntity : class, IFileMetadataConfigRoot
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(fileStorageService);

        validator.RuleFor(m => m.Files)
                 .NotNull()
                 .Must(
                      (entity, _, context) =>
                      {
                          var (isValid, errors) = fileStorageService.ValidateFileTypes<TEntity>(entity).Result;
                          foreach (var err in errors)
                              context.AddFailure(err);

                          var i = 0;
                          foreach (var file in entity.Files)
                          {
                              var (isFileValid, fileErrors) = fileStorageService.ValidateFile<TEntity>(entity, file).Result;
                              if (!isFileValid)
                                  foreach (var err in fileErrors)
                                      context.AddFailure($"[{i}]", err);

                              i++;
                          }

                          return isValid;
                      }
                  );
    }
}