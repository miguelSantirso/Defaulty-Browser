using Defaulty.Core;

namespace Defaulty;

public sealed class CoreServices
{
    public IRegistryReader Registry { get; }
    public IIndirectStringResolver IndirectStrings { get; }
    public IProgIdResolver ProgIds { get; }
    public IFileSystem FileSystem { get; }
    public CapabilitiesReader Capabilities { get; }
    public RegisteredApplicationSource Source { get; }
    public BrowserCatalog Catalog { get; }
    public DefaultAssociationReader AssociationReader { get; }
    public IOsVersionProvider OsVersion { get; }
    public IProcessStarter ProcessStarter { get; }
    public SettingsLauncher SettingsLauncher { get; }

    public CoreServices()
    {
        Registry = new WindowsRegistryReader();
        IndirectStrings = new ShLoadIndirectStringResolver();
        ProgIds = new WindowsProgIdResolver();
        FileSystem = new SystemFileSystem();
        Capabilities = new CapabilitiesReader(Registry, IndirectStrings, ProgIds, FileSystem);
        Source = new RegisteredApplicationSource(Registry, Capabilities);
        Catalog = new BrowserCatalog();
        AssociationReader = new DefaultAssociationReader(new WindowsAssocQueryString());
        OsVersion = new WindowsOsVersionProvider(Registry);
        ProcessStarter = new WindowsProcessStarter();
        SettingsLauncher = new SettingsLauncher(OsVersion, ProcessStarter);
    }
}