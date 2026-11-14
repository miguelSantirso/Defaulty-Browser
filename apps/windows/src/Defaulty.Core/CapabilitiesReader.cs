using System.IO;

namespace Defaulty.Core;

public sealed class CapabilitiesReader
{
    private readonly IRegistryReader _registry;
    private readonly IIndirectStringResolver _indirectStrings;
    private readonly IProgIdResolver _progIds;
    private readonly IFileSystem _fileSystem;

    public CapabilitiesReader(
        IRegistryReader registry,
        IIndirectStringResolver indirectStrings,
        IProgIdResolver progIds,
        IFileSystem? fileSystem = null)
    {
        _registry = registry;
        _indirectStrings = indirectStrings;
        _progIds = progIds;
        _fileSystem = fileSystem ?? new SystemFileSystem();
    }

    public Browser? Read(
        string registrationName,
        string capabilitiesPath,
        RegistrationHive hive,
        RegistryViewKind view)
    {
        var normalizedPath = NormalizePath(capabilitiesPath);
        bool capabilitiesExists = _registry.KeyExists(normalizedPath, hive, view);
        if (!capabilitiesExists)
        {
            return null;
        }

        // ApplicationName
        var rawName = _registry.GetStringValue(normalizedPath, "ApplicationName", hive, view);
        var hiddenValue = _registry.GetDWordValue(normalizedPath, "Hidden", hive, view);
        bool hidden = hiddenValue == 1;

        // UrlAssociations
        var urlAssocPath = normalizedPath + "\\UrlAssociations";
        var progIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (_registry.KeyExists(urlAssocPath, hive, view))
        {
            foreach (var kv in _registry.EnumerateValues(urlAssocPath, hive, view))
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                {
                    progIds[kv.Key] = kv.Value;
                }
            }
        }

        // Require http and/or https
        bool hasHttp = progIds.ContainsKey("http");
        bool hasHttps = progIds.ContainsKey("https");
        if (!hasHttp && !hasHttps)
        {
            return null;
        }

        // Primary progId: prefer https then http
        string? primaryProgId = progIds.TryGetValue("https", out var hp) ? hp
            : (progIds.TryGetValue("http", out var http) ? http : progIds.Values.FirstOrDefault());

        // Resolve display name
        string? displayName = _indirectStrings.Resolve(rawName);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = null;
        }

        // Executable path + icon
        string? executablePath = primaryProgId is not null ? _progIds.GetExecutablePath(primaryProgId) : null;
        bool executableExists = executablePath is null || _fileSystem.FileExists(executablePath);

        string? iconReference = primaryProgId is not null ? _progIds.GetDefaultIcon(primaryProgId) : null;
        if (string.IsNullOrWhiteSpace(iconReference))
        {
            iconReference = executablePath;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                displayName = Path.GetFileNameWithoutExtension(executablePath);
            }
            else
            {
                displayName = registrationName;
            }
        }

        var supportsPerApp = (hive == RegistrationHive.User) || (view == RegistryViewKind.Native64);

        var id = (hive == RegistrationHive.Machine ? "machine:" : "user:") + registrationName;

        return new Browser(
            Id: id,
            DisplayName: displayName!,
            IconReference: iconReference,
            ExecutablePath: executablePath,
            PrimaryProgId: primaryProgId,
            ProgIds: progIds,
            RegistrationName: registrationName,
            Hive: hive,
            View: view,
            Hidden: hidden,
            CapabilitiesExists: true,
            ExecutableExists: executableExists,
            IsSynthetic: false,
            SupportsPerAppSettingsLink: supportsPerApp);
    }

    public Browser? ReadCapabilities(
        string registrationName,
        string capabilitiesPath,
        RegistrationHive hive,
        RegistryViewKind view) => Read(registrationName, capabilitiesPath, hive, view);

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }
        var trimmed = path.Trim();
        // Strip hive prefixes if present
        foreach (var prefix in new[] { "HKEY_LOCAL_MACHINE\\", "HKEY_CURRENT_USER\\", "HKLM\\", "HKCU\\" })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(prefix.Length);
                break;
            }
        }
        return trimmed;
    }
}