using System.Text;

namespace Defaulty.Core;

public static class Cli
{
    public const string Usage =
        "Defaulty - switch the default web browser via Windows Settings.\n" +
        "Usage:\n" +
        "  Defaulty.exe --list          List registered browsers (* marks https default)\n" +
        "  Defaulty.exe --current       Print https and http associations\n" +
        "  Defaulty.exe --diagnose      Print OS build/UBR, registrations, state, link mode\n" +
        "  Defaulty.exe --help          Show this help\n" +
        "  Defaulty.exe --open-settings Open the Default apps page (manual only)\n" +
        "\n" +
        "--set-default is not supported on Windows: there is no supported silent setter.";

    public static string FormatList(IReadOnlyList<Browser> browsers, Browser? httpsDefault)
    {
        var sb = new StringBuilder();
        foreach (var b in browsers)
        {
            string marker = (httpsDefault is not null && b.Id == httpsDefault.Id) ? "* " : string.Empty;
            string progId = b.PrimaryProgId ?? string.Empty;
            string exe = b.ExecutablePath ?? string.Empty;
            sb.Append(marker);
            sb.Append(b.Id);
            sb.Append('\t');
            sb.Append(b.DisplayName);
            sb.Append('\t');
            sb.Append(progId);
            sb.Append('\t');
            sb.Append(exe);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string FormatCurrent(CurrentAssociationState state)
    {
        var sb = new StringBuilder();
        if (state.HttpsResolved)
        {
            sb.Append("https=");
            sb.Append(state.Https.ProgId ?? "none");
            sb.Append('\t');
            sb.Append(state.Https.FriendlyName ?? state.Https.ExecutablePath ?? string.Empty);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("https=none");
        }
        if (state.HttpResolved)
        {
            sb.Append("http=");
            sb.Append(state.Http.ProgId ?? "none");
            sb.Append('\t');
            sb.Append(state.Http.FriendlyName ?? state.Http.ExecutablePath ?? string.Empty);
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("http=none");
        }
        return sb.ToString();
    }

    public static bool IsSetDefault(string arg)
    {
        return string.Equals(arg, "--set-default", StringComparison.OrdinalIgnoreCase);
    }
}