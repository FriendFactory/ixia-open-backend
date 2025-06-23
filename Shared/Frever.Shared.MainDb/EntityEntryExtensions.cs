using System.Linq;
using Common.Models.Database.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Frever.Shared.MainDb;

internal static class EntityEntryExtensions
{
    private const string CreatePropertyName = nameof(ITimeChangesTrackable.CreatedTime);
    private const string ModifiedPropertyName = nameof(ITimeChangesTrackable.ModifiedTime);

    public static bool HasCreateDateProperty(this EntityEntry entry)
    {
        return entry.Properties.Any(x => x.Metadata.Name == CreatePropertyName);
    }

    public static bool HasModifiedDateProperty(this EntityEntry entry)
    {
        return entry.Properties.Any(x => x.Metadata.Name == ModifiedPropertyName);
    }
}