namespace Defaulty.Core;

public sealed class RegisteredApplicationSource
{
    private readonly IRegistryReader _registry;
    private readonly CapabilitiesReader _capabilities;

    public RegisteredApplicationSource(IRegistryReader registry, CapabilitiesReader capabilities)
    {
        _registry = registry;
        _capabilities = capabilities;
    }

    public IReadOnlyList<Browser> Discover()
    {
        var results = new List<Browser>();

        // Machine: 64-bit and 32-bit views
        foreach (var view in new[] { RegistryViewKind.Native64, RegistryViewKind.Wow64 })
        {
            Collect(RegistrationHive.Machine, view, results);
        }

        // User: HKCU view
        Collect(RegistrationHive.User, RegistryViewKind.Native64, results);

        return results;
    }

    public IReadOnlyList<Browser> Enumerate() => Discover();

    public IReadOnlyList<Browser> GetBrowsers() => Discover();

    private void Collect(RegistrationHive hive, RegistryViewKind view, List<Browser> results)
    {
        const string registeredAppsPath = "SOFTWARE\\RegisteredApplications";
        if (!_registry.KeyExists(registeredAppsPath, hive, view))
        {
            return;
        }
        foreach (var kv in _registry.EnumerateValues(registeredAppsPath, hive, view))
        {
            var registrationName = kv.Key;
            var capabilitiesPath = kv.Value;
            if (string.IsNullOrWhiteSpace(registrationName) || string.IsNullOrWhiteSpace(capabilitiesPath))
            {
                continue;
            }
            var browser = _capabilities.Read(registrationName, capabilitiesPath, hive, view);
            if (browser is not null)
            {
                results.Add(browser);
            }
        }
    }
}