using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AuthServerShared.PhoneNormalization;

public class PhoneNormalizationService : IPhoneNormalizationService
{
    private static readonly Regex SwedishPhoneNumberFormat = new(@"^((\+)46|46)7[\d]{8}", RegexOptions.Compiled | RegexOptions.Compiled);

    public Task<string> FormatPhoneNumber(string phoneNumber)
    {
        //temporary validation for swedish phones only
        if (phoneNumber.StartsWith("46"))
            phoneNumber = $"+{phoneNumber}";

        if (phoneNumber.StartsWith("+46") && !SwedishPhoneNumberFormat.IsMatch(phoneNumber))
        {
            var indexOfFirstZero = phoneNumber.IndexOf('0');

            if (indexOfFirstZero.Equals(3))
                phoneNumber = phoneNumber.Remove(indexOfFirstZero, 1);
        }

        return Task.FromResult(phoneNumber);
    }
}