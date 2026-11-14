using System.Diagnostics;
using Defaulty.Core;

namespace Defaulty;

public sealed class WindowsProcessStarter : IProcessStarter
{
    public bool Start(string uri)
    {
        try
        {
            var psi = new ProcessStartInfo(uri)
            {
                UseShellExecute = true,
            };
            using var process = Process.Start(psi);
            // For shell-executed URIs/protocols (e.g. ms-settings:), Process.Start may
            // return null on success because the request is handed off to an existing handler.
            return true;
        }
        catch
        {
            return false;
        }
    }
}