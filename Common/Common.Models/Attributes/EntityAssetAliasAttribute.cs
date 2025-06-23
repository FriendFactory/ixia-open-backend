using System;

namespace Common.Models.Attributes;

/// <summary>
///     Allows to download files for entity using
///     name different from entity class name.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EntityAssetAliasAttribute : Attribute
{
    /// <summary>
    ///     Allows to download files for entity using name different from class name.
    /// </summary>
    /// <param name="aliases">Comma-separated list of aliases.</param>
    /// <exception cref="ArgumentException">aliases null or empty or whitespace.</exception>
    public EntityAssetAliasAttribute(string aliases)
    {
        if (string.IsNullOrWhiteSpace(aliases))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(aliases));
        Aliases = aliases.Split(".");
    }

    public string[] Aliases { get; }
}