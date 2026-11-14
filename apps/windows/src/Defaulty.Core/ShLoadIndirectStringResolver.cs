using System.Runtime.InteropServices;

namespace Defaulty.Core;

public sealed class ShLoadIndirectStringResolver : IIndirectStringResolver
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHLoadIndirectString(string pszSource, System.Text.StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

    public string? Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (!value.TrimStart().StartsWith("@", StringComparison.Ordinal))
        {
            return value.Trim();
        }

        var sb = new System.Text.StringBuilder(1024);
        int hr = SHLoadIndirectString(value, sb, sb.Capacity, IntPtr.Zero);
        if (hr < 0 || sb.Length == 0)
        {
            return null;
        }
        return sb.ToString();
    }
}