namespace Defaulty.Core;

public sealed class WindowsOsVersionProvider : IOsVersionProvider
{
    private readonly IRegistryReader _registry;

    public WindowsOsVersionProvider(IRegistryReader? registry = null)
    {
        _registry = registry ?? new WindowsRegistryReader();
    }

    public OsVersionInfo GetOsVersion()
    {
        int build = Environment.OSVersion.Version.Build;
        int ubr = 0;
        try
        {
            var value = _registry.GetDWordValue(
                "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
                "UBR",
                RegistrationHive.Machine,
                RegistryViewKind.Native64);
            ubr = value ?? 0;
        }
        catch
        {
            ubr = 0;
        }
        return new OsVersionInfo(build, ubr);
    }
}