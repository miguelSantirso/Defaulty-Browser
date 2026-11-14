namespace Defaulty.Core;

/// <summary>File system abstraction for testability.</summary>
public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
}

public sealed class SystemFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
}

/// <summary>Resolves resource-backed indirect strings (e.g. @dll,-id).</summary>
public interface IIndirectStringResolver
{
    string? Resolve(string? value);
}

/// <summary>Reads registry values across hives/views.</summary>
public interface IRegistryReader
{
    bool KeyExists(string keyPath, RegistrationHive hive, RegistryViewKind view);
    IEnumerable<KeyValuePair<string, string>> EnumerateValues(string keyPath, RegistrationHive hive, RegistryViewKind view);
    string? GetStringValue(string keyPath, string valueName, RegistrationHive hive, RegistryViewKind view);
    int? GetDWordValue(string keyPath, string valueName, RegistrationHive hive, RegistryViewKind view);
}

/// <summary>Resolves DefaultIcon and executable from a ProgId.</summary>
public interface IProgIdResolver
{
    string? GetDefaultIcon(string progId);
    string? GetExecutablePath(string progId);
}

/// <summary>OS build/UBR provider.</summary>
public interface IOsVersionProvider
{
    OsVersionInfo GetOsVersion();
}

public sealed record OsVersionInfo(int Build, int Ubr);

/// <summary>Process starter for Settings URIs (testable).</summary>
public interface IProcessStarter
{
    bool Start(string uri);
}

/// <summary>AssocQueryString abstraction.</summary>
public interface IAssocQueryString
{
    string? Query(AssocF flags, AssocStr str, string assoc);
}

public enum AssocF : uint
{
    None = 0,
    IsProtocol = 0x1000,
    AppToApp = 0x10000,
}

public enum AssocStr
{
    Executable = 2,
    FriendlyAppName = 4,
    ProgId = 20,
    AppId = 21,
    AppIconReference = 23,
}