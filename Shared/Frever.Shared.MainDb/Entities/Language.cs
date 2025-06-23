using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Language : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets 3-letter Language ISO code.
    ///     Use ISO code in video db to avoid Language table synchronization.
    /// </summary>
    public string IsoCode { get; set; }

    public string ISO2Code { get; set; }

    public bool AvailableForCrew { get; set; }
    public virtual ICollection<Group> Groups { get; set; }
}