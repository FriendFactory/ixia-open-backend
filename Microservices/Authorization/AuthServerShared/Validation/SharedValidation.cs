using System.Linq;
using System.Threading.Tasks;

namespace AuthServerShared.Validation;

public static class SharedValidation
{
    public static async Task<ValidationResponse> IsNicknameProperlyFormed(string nickname)
    {
        var request = new NicknameRequest {Nickname = nickname};

        var validator = new NicknameRequestValidator();

        var result = await validator.ValidateAsync(request);

        return new ValidationResponse {IsValid = result.IsValid, ValidationError = result.Errors.FirstOrDefault()?.ToString()};
    }
}