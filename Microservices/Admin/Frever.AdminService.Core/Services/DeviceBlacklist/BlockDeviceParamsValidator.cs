using FluentValidation;

namespace Frever.AdminService.Core.Services.DeviceBlacklist;

public class BlockDeviceParamsValidator : AbstractValidator<BlockDeviceParams>
{
    public BlockDeviceParamsValidator()
    {
        RuleFor(e => e.DeviceId).NotEmpty().MinimumLength(1);
        RuleFor(e => e.Reason).NotEmpty().MinimumLength(1);
    }
}