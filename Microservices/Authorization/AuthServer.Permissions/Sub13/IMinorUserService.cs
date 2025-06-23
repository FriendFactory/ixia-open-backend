using System;
using System.Threading.Tasks;
using AuthServer.Permissions.DataAccess;
using Common.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Permissions.Sub13;

public interface IMinorUserService
{
    Task<bool> IsMinorAge(string countryCode, DateTime dateOfBirth);
    Task<bool> NeedsExtendedEmailVerification(long countryId);
}

public class MinorUserService(IMainGroupRepository repo) : IMinorUserService
{
    private readonly IMainGroupRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public async Task<bool> IsMinorAge(string countryCode, DateTime dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(countryCode));

        var countryEntity = await _repo.FindCountryByCode(countryCode).FirstOrDefaultAsync();
        if (countryEntity == null)
            throw AppErrorWithStatusCodeException.BadRequest("Country with such code not found", "CountryNotFound");

        return IsMinorAge(dateOfBirth, countryEntity.AgeOfConsent);
    }

    public async Task<bool> NeedsExtendedEmailVerification(long countryId)
    {
        var country = await _repo.FindCountryById(countryId).FirstOrDefaultAsync();

        return country.ExtendedParentAgeValidation;
    }

    public async Task<bool> IsMinorAge(long countryId, DateTime dateOfBirth)
    {
        var countryEntity = await _repo.FindCountryById(countryId).FirstOrDefaultAsync();
        if (countryEntity == null)
            throw AppErrorWithStatusCodeException.BadRequest("Country with such code not found", "CountryNotFound");

        return IsMinorAge(dateOfBirth, countryEntity.AgeOfConsent);
    }

    public bool IsMinorAge(DateTime dateOfBirth, int ageOfConsent)
    {
        var age = DateTime.Now.Date - dateOfBirth.Date;
        var fullYears = age.TotalDays / 365;

        return fullYears <= ageOfConsent;
    }
}