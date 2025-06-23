using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class Country : IEntity, IAdminCategory
{
    public Country()
    {
        Group = new HashSet<Group>();
    }

    public long Id { get; set; }
    public string ISOName { get; set; }
    public string ISO2Code { get; set; }
    public string DisplayName { get; set; }
    public string MobileNumberPrefix { get; set; }
    public bool EnableMusic { get; set; }
    public int AgeOfConsent { get; set; }
    public bool StrictCoppaRules { get; set; }
    public bool ExtendedParentAgeValidation { get; set; }
    public bool AvailableForMarketing { get; set; }

    public virtual ICollection<Group> Group { get; set; }
}