using System.Threading.Tasks;

namespace AuthServerShared.PhoneNormalization;

public interface IPhoneNormalizationService
{
    Task<string> FormatPhoneNumber(string phoneNumber);
}