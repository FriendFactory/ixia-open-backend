using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Localizations;

public interface ILocalizationRepository
{
    IQueryable<Country> GetCountries();

    IQueryable<Language> GetLanguages();

    IQueryable<Localization> GetLocalizations();
}

internal sealed class PersistentLocalizationRepository(IReadDb db) : ILocalizationRepository
{
    public IQueryable<Country> GetCountries()
    {
        return db.Country.AsNoTracking();
    }

    public IQueryable<Language> GetLanguages()
    {
        return db.Language.AsNoTracking();
    }

    public IQueryable<Localization> GetLocalizations()
    {
        return db.Localization.AsNoTracking();
    }
}