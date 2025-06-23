using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable CS0618

namespace Common.Infrastructure.Database;

public static class EntityModelExtensions
{
    public static bool IsManyToMany(this INavigation nav)
    {
        ArgumentNullException.ThrowIfNull(nav);

        return nav.TargetEntityType.FindPrimaryKey().Properties.Count > 1;
    }

    public static ManyToManyKeyInfo GetManyToManyKeyInfo(this INavigation nav)
    {
        if (!nav.IsManyToMany())
            return null;

        var mainSideFk = nav.ForeignKey.DependentToPrincipal.ForeignKey;
        var otherSideFk = nav.TargetEntityType.GetProperties()
                             .Where(p => p.IsPrimaryKey())
                             .SelectMany(p => p.GetContainingForeignKeys())
                             .Single(fk => fk != mainSideFk);

        return new ManyToManyKeyInfo
               {
                   MainSideType = nav.ForeignKey.PrincipalEntityType.ClrType,
                   MainSideProperty = mainSideFk.Properties.Single(),
                   MainSideForeignKey = mainSideFk,
                   OtherSideProperty = otherSideFk.Properties.Single(),
                   OtherSideForeignKey = otherSideFk,
                   OtherSideType = otherSideFk.DependentToPrincipal.ClrType
               };
    }
}

public class ManyToManyKeyInfo
{
    public Type MainSideType { get; set; }

    public IProperty MainSideProperty { get; set; }

    public IForeignKey MainSideForeignKey { get; set; }

    public IProperty OtherSideProperty { get; set; }

    public IForeignKey OtherSideForeignKey { get; set; }

    public Type OtherSideType { get; set; }
}