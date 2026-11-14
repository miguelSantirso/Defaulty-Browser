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
            return process is not null;
        }
        catch
        {
            return false;
        }
    }
}