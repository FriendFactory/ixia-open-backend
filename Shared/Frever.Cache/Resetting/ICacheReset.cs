using System;
using System.Threading.Tasks;

namespace Frever.Cache.Resetting;

/// <summary>
///     Allows to reset cache either by key or by entity dependency tracking.
///     Methods resets either shared and individual user cache.
/// </summary>
public interface ICacheReset
{
    Task ResetKeys(params string[] keys);

    Task ResetOnDependencyChange(Type entity, long? currentGroupId);
}