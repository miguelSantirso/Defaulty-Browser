using System.Runtime.InteropServices;
using System.Text;

namespace Defaulty.Core;

public sealed class WindowsAssocQueryString : IAssocQueryString
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int AssocQueryStringW(
        AssocF flags,
        AssocStr str,
        string pszAssoc,
        string? pszExtra,
        StringBuilder? pszOut,
        ref uint pcchOut);

    public string? Query(AssocF flags, AssocStr str, string assoc)
    {
        uint size = 0;
        int hr = AssocQueryStringW(flags, str, assoc, null, null, ref size);
        if (hr != 1) // S_FALSE? Actually returns S_FALSE with required size; handle below
        {
            // If size is 0, failure
            if (size == 0)
            {
                // Try direct error
            }
        }
        if (size == 0)
        {
            return null;
        }
        var sb = new StringBuilder((int)size);
        hr = AssocQueryStringW(flags, str, assoc, null, sb, ref size);
        if (hr < 0)
        {
            return null;
        }
        var result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}