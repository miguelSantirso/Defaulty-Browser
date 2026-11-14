using System.Collections.Generic;

namespace Defaulty.Core;

/// <summary>
/// Hive that supplied a browser registration.
/// </summary>
public enum RegistrationHive
{
    Machine,
    User,
}

/// <summary>
/// Registry view that supplied a machine registration.
/// </summary>
public enum RegistryViewKind
{
    Native64,
    Wow64,
}

/// <summary>
/// A web handler discovered on the system.
/// </summary>
public sealed record Browser(
    string Id,
    string DisplayName,
    string? IconReference = null,
    string? ExecutablePath = null,
    string? PrimaryProgId = null,
    IReadOnlyDictionary<string, string>? ProgIds = null,
    string? RegistrationName = null,
    RegistrationHive Hive = RegistrationHive.Machine,
    RegistryViewKind View = RegistryViewKind.Native64,
    bool Hidden = false,
    bool CapabilitiesExists = true,
    bool ExecutableExists = true,
    bool IsSynthetic = false,
    bool SupportsPerAppSettingsLink = true)
{
    public IReadOnlyDictionary<string, string> EffectiveProgIds =>
        ProgIds ?? new Dictionary<string, string>();

    public bool IsStale => !CapabilitiesExists || (ExecutablePath is not null && !ExecutableExists);

    /// <summary>Alias for <see cref="DisplayName"/>.</summary>
    public string Name => DisplayName;
}