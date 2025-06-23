using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;

public class CountryCodeLookup(IReadDb mainDb)
{
    private static readonly object SyncRoot = new();

    private static Dictionary<long, Country> _countries;
    private static Dictionary<string, string> _countryLookupDictionary;
    private static readonly HashSet<string> MusicAllowedCountries = new(StringComparer.OrdinalIgnoreCase);

    private readonly IReadDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    public async Task<Country> GetById(long countryId)
    {
        await GetCountryLookup();
        _countries.TryGetValue(countryId, out var result);
        return result;
    }

    public async Task<HashSet<string>> WithEnabledMusic()
    {
        await GetCountryLookup();

        return new HashSet<string>(MusicAllowedCountries, StringComparer.OrdinalIgnoreCase);
    }

    public Task<Dictionary<string, string>> GetCountryLookup()
    {
        if (_countryLookupDictionary != null)
            return Task.FromResult(_countryLookupDictionary);

        lock (SyncRoot)
        {
            if (_countryLookupDictionary == null)
            {
                var countries = _mainDb.Country.AsNoTracking().ToArray();
                _countries = countries.ToDictionary(c => c.Id);

                _countryLookupDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in countries)
                    _countryLookupDictionary.Add(c.ISO2Code.ToLower(), c.ISOName.ToLower());
                MusicAllowedCountries.Clear();
                foreach (var c in countries.Where(c => c.EnableMusic))
                    MusicAllowedCountries.Add(c.ISOName);
            }
        }

        return Task.FromResult(_countryLookupDictionary);
    }

    public async Task<string[]> ToIso3(IEnumerable<string> iso2)
    {
        if (iso2 == null)
            return [];

        var lookup = await GetCountryLookup();

        var result = new List<string>();
        foreach (var countryCode in iso2.Select(i => i.Trim()))
            if (countryCode.Length == 2)
            {
                if (lookup.TryGetValue(countryCode, out var i3))
                    result.Add(i3);
            }
            else
            {
                result.Add(countryCode);
            }

        return result.ToArray();
    }

    public async Task<bool> IsMusicEnabled(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(iso));

        if (iso.Length == 2)
        {
            var iso3 = (await ToIso3([iso])).FirstOrDefault();
            if (!string.IsNullOrEmpty(iso3))
                return await IsMusicEnabled(iso3);
        }

        await GetCountryLookup();

        return MusicAllowedCountries.Contains(iso);
    }
}