using Microsoft.Win32;

namespace Defaulty.Core;

public sealed class WindowsRegistryReader : IRegistryReader
{
    public bool KeyExists(string keyPath, RegistrationHive hive, RegistryViewKind view)
    {
        using var key = OpenKey(keyPath, hive, view, writable: false);
        return key is not null;
    }

    public IEnumerable<KeyValuePair<string, string>> EnumerateValues(string keyPath, RegistrationHive hive, RegistryViewKind view)
    {
        using var key = OpenKey(keyPath, hive, view, writable: false);
        if (key is null)
        {
            yield break;
        }
        foreach (var name in key.GetValueNames())
        {
            var value = key.GetValue(name);
            yield return new KeyValuePair<string, string>(name, value?.ToString() ?? string.Empty);
        }
    }

    public string? GetStringValue(string keyPath, string valueName, RegistrationHive hive, RegistryViewKind view)
    {
        using var key = OpenKey(keyPath, hive, view, writable: false);
        return key?.GetValue(valueName)?.ToString();
    }

    public int? GetDWordValue(string keyPath, string valueName, RegistrationHive hive, RegistryViewKind view)
    {
        using var key = OpenKey(keyPath, hive, view, writable: false);
        if (key is null)
        {
            return null;
        }
        var value = key.GetValue(valueName);
        if (value is int i)
        {
            return i;
        }
        if (value is not null && int.TryParse(value.ToString(), out var parsed))
        {
            return parsed;
        }
        return null;
    }

    private static RegistryKey? OpenKey(string keyPath, RegistrationHive hive, RegistryViewKind view, bool writable)
    {
        var baseHive = hive == RegistrationHive.Machine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        var registryView = view == RegistryViewKind.Native64 ? RegistryView.Registry64 : RegistryView.Registry32;
        var baseKey = RegistryKey.OpenBaseKey(baseHive, registryView);
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            return baseKey;
        }
        return baseKey.OpenSubKey(keyPath, writable);
    }
}