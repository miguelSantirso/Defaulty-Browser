using System.Globalization;
using System.Web;

namespace Defaulty.Core;

public sealed class SettingsLauncher
{
    private readonly IOsVersionProvider _osVersion;
    private readonly IProcessStarter _processStarter;

    public SettingsLauncher(IOsVersionProvider osVersion, IProcessStarter processStarter)
    {
        _osVersion = osVersion;
        _processStarter = processStarter;
    }

    public const string BaseUri = "ms-settings:defaultapps";

    /// <summary>
    /// Returns true when the OS build supports per-app Default apps links.
    /// </summary>
    public bool IsPerAppOsSupported()
    {
        var os = _osVersion.GetOsVersion();
        int build = os.Build;
        int ubr = os.Ubr;

        // Windows 11 23H2 and later: build 22631+
        if (build >= 22631)
        {
            return true;
        }
        // 22H2: build 22621, UBR >= 1555
        if (build == 22621)
        {
            return ubr >= 1555;
        }
        // 21H2: build 22000, UBR >= 1817
        if (build == 22000)
        {
            return ubr >= 1817;
        }
        return false;
    }

    /// <summary>
    /// Returns true when a per-app link can be used for this registration.
    /// </summary>
    public bool SupportsPerAppLink(Browser browser)
    {
        if (browser is null || string.IsNullOrWhiteSpace(browser.RegistrationName))
        {
            return false;
        }
        if (!browser.SupportsPerAppSettingsLink)
        {
            return false;
        }
        return IsPerAppOsSupported();
    }

    public bool IsPerAppSupported(Browser browser) => SupportsPerAppLink(browser);

    public bool IsPerAppSupported() => IsPerAppOsSupported();

    public string BuildUri(Browser browser) => GetSettingsUri(browser);

    public string GetSettingsUrl(Browser browser) => GetSettingsUri(browser);

    public string GetSettingsUri(Browser browser)
    {
        if (SupportsPerAppLink(browser))
        {
            var escaped = Uri.EscapeDataString(browser.RegistrationName!);
            var param = browser.Hive == RegistrationHive.Machine
                ? $"registeredAppMachine={escaped}"
                : $"registeredAppUser={escaped}";
            return $"{BaseUri}?{param}";
        }
        return BaseUri;
    }

    public bool Launch(Browser browser)
    {
        var uri = GetSettingsUri(browser);
        try
        {
            return _processStarter.Start(uri);
        }
        catch
        {
            return false;
        }
    }

    public bool LaunchDefaultApps()
    {
        try
        {
            return _processStarter.Start(BaseUri);
        }
        catch
        {
            return false;
        }
    }
}