using FluentValidation;
using Frever.AdminService.Core.Services.StorageFiles.Contracts;

namespace Frever.AdminService.Core.Services.StorageFiles;

public class UploadStorageFileModelValidator : AbstractValidator<UploadStorageFileModel>
{
    public UploadStorageFileModelValidator()
    {
        RuleFor(e => e.Key).NotEmpty().Matches(@"^[a-zA-Z0-9_/]+$");
        RuleFor(e => e.UploadId).NotEmpty();
    }
}