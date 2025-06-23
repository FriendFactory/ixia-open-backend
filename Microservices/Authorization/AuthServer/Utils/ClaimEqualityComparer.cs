using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace AuthServer.Utils;

internal class ClaimEqualityComparer : IEqualityComparer<Claim>
{
    public bool Equals(Claim x, Claim y)
    {
        return (x != null && y != null && x.Type.Equals(y.Type, StringComparison.OrdinalIgnoreCase) &&
                x.Value.Equals(y.Value, StringComparison.OrdinalIgnoreCase)) || (x == null && y == null);
    }

    public int GetHashCode(Claim obj)
    {
        var value = obj.Value + "_" + obj.Type;

        return value.GetHashCode();
    }
}