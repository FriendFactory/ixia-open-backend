using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AuthServerShared;
using AuthServerShared.PhoneNormalization;
using Frever.Client.Core.Features.Social.DataAccess;
using Frever.Client.Shared.Social.Services;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Social.Profiles;

public interface IPhoneLookupService
{
    /// <summary>
    ///     Gets the array of the phones from user's address book
    ///     and tries to lookup frever users using their phone numbers.
    /// </summary>
    Task<PhoneLookupInfo[]> LookupPhones(string[] phones);
}

public class PhoneLookupInfo
{
    public string ProvidedPhoneNumber { get; set; }

    public string FreverProfilePhone { get; set; }

    public long GroupId { get; set; }

    public string GroupNickname { get; set; }

    public DateTime RegistrationDate { get; set; }

    public bool IsFollowing { get; set; }
}

public class DefaultPhoneLookupService : IPhoneLookupService
{
    private static readonly Regex NonDigits = new(@"[^\d]", RegexOptions.Compiled | RegexOptions.Compiled);
    private readonly UserInfo _currentUser;
    private readonly IMainDbRepository _mainDbRepository;
    private readonly IPhoneNormalizationService _phoneNormalizationService;
    private readonly ISocialSharedService _socialSharedService;

    public DefaultPhoneLookupService(
        UserInfo currentUser,
        IMainDbRepository mainDbRepository,
        IPhoneNormalizationService phoneNormalizationService,
        ISocialSharedService socialSharedService
    )
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mainDbRepository = mainDbRepository ?? throw new ArgumentNullException(nameof(mainDbRepository));
        _phoneNormalizationService = phoneNormalizationService ?? throw new ArgumentNullException(nameof(phoneNormalizationService));
        _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));
    }

    public async Task<PhoneLookupInfo[]> LookupPhones(string[] phones)
    {
        if (phones == null)
            throw new ArgumentNullException(nameof(phones));

        phones = phones.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

        if (phones.Length == 0)
            return new PhoneLookupInfo[] { };

        var defaultPrefix = await GetDefaultMobileNumberPrefix();

        var phoneCount = 100;
        var normalizedPhones = (await Task.WhenAll(phones.Select(ph => NormalizePhone(ph))))
                              .Where(a => a.NormalizedNumber.Length >= 8) // Filter out too short numbers to avoid data exposure
                              .ToArray()
                              .AsEnumerable();

        var result = new List<PhoneLookupInfo>();
        while (normalizedPhones.Any())
        {
            var part = normalizedPhones.Take(phoneCount);
            var lookup = await LookupNormalizedPhones(part, defaultPrefix);
            result.AddRange(lookup);

            normalizedPhones = normalizedPhones.Skip(phoneCount);
        }

        return result.ToArray();
    }

    private async Task<NormalizedPhone> NormalizePhone(string phone)
    {
        var normalized = NonDigits.Replace(phone, "", phone.Length);

        while (normalized.Length >= 8 && normalized.StartsWith("0"))
            normalized = normalized.Substring(1);

        normalized = await _phoneNormalizationService.FormatPhoneNumber(normalized);

        return new NormalizedPhone {NormalizedNumber = normalized, SourceNumber = phone};
    }

    private async Task<PhoneLookupInfo[]> LookupNormalizedPhones(IEnumerable<NormalizedPhone> phoneNumbers, string defaultCountryPrefix)
    {
        var blocked = await _socialSharedService.GetBlocked(_currentUser.UserMainGroupId);
        var numbersOnly = phoneNumbers.Select(a => a.NormalizedNumber).ToArray();
        var result = await _mainDbRepository.FindProfilesByPhone(numbersOnly, _currentUser.UserMainGroupId, blocked);

        return result.Select(
                          a => new PhoneLookupInfo
                               {
                                   GroupId = a.GroupId,
                                   GroupNickname = a.GroupNickName,
                                   FreverProfilePhone = a.PhoneNumber,
                                   ProvidedPhoneNumber = ChooseFromMatchedPhoneNumbers(phoneNumbers, a, defaultCountryPrefix),
                                   IsFollowing = a.IsFollowing,
                                   RegistrationDate = a.RegistrationDate
                               }
                      )
                     .ToArray();
    }

    private static string ChooseFromMatchedPhoneNumbers(
        IEnumerable<NormalizedPhone> phoneNumbers,
        GroupInfo group,
        string defaultCountryPrefix
    )
    {
        return phoneNumbers.Where(i => group.PhoneNumber.EndsWith(i.NormalizedNumber))
                           .OrderByDescending(a => a.NormalizedNumber.StartsWith(defaultCountryPrefix) ? 100 : 0)
                           .Select(e => e.SourceNumber)
                           .First();
    }

    private async Task<string> GetDefaultMobileNumberPrefix()
    {
        var userPhoneNumber = await _mainDbRepository.GetUserById(_currentUser.UserId).Select(u => u.PhoneNumber).FirstOrDefaultAsync();
        var countryMobileNumberPrefixes = await _mainDbRepository.GetCountries().Select(c => c.MobileNumberPrefix).ToArrayAsync();

        if (string.IsNullOrWhiteSpace(userPhoneNumber))
            return string.Empty;

        return countryMobileNumberPrefixes.FirstOrDefault(c => userPhoneNumber.StartsWith("+" + c)) ?? string.Empty;
    }

    private class NormalizedPhone
    {
        public string SourceNumber { get; set; }

        public string NormalizedNumber { get; set; }
    }
}