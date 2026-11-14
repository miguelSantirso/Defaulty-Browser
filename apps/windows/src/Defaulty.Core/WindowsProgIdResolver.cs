using Microsoft.Win32;

namespace Defaulty.Core;

public sealed class WindowsProgIdResolver : IProgIdResolver
{
    public string? GetDefaultIcon(string progId)
    {
        if (string.IsNullOrWhiteSpace(progId))
        {
            return null;
        }
        using var key = Registry.ClassesRoot.OpenSubKey(progId + "\\DefaultIcon");
        return key?.GetValue(null)?.ToString();
    }

    public string? GetExecutablePath(string progId)
    {
        if (string.IsNullOrWhiteSpace(progId))
        {
            return null;
        }
        using var key = Registry.ClassesRoot.OpenSubKey(progId + "\\shell\\open\\command");
        var command = key?.GetValue(null)?.ToString();
        return ParseExecutable(command);
    }

    private static string? ParseExecutable(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }
        command = command.Trim();
        if (command.StartsWith("\"", StringComparison.Ordinal))
        {
            var end = command.IndexOf('"', 1);
            if (end > 1)
            {
                return command.Substring(1, end - 1);
            }
        }
        // First token
        var space = command.IndexOf(' ');
        return space > 0 ? command.Substring(0, space) : command;
    }
}