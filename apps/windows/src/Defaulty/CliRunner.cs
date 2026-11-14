using System;
using Defaulty.Core;

namespace Defaulty;

internal static class CliRunner
{
    public static int Run(string[] args, CoreServices? services = null)
    {
        services ??= new CoreServices();

        if (args.Length == 0)
        {
            return 0;
        }

        string cmd = args[0];

        switch (cmd)
        {
            case "--help":
            case "-h":
            case "/?":
                Console.WriteLine(Cli.Usage);
                return 0;

            case "--list":
                {
                    var browsers = services.Catalog.Normalize(services.Source.Discover());
                    var state = services.AssociationReader.ReadCurrent();
                    Browser? httpsDefault = state.HttpsResolved
                        ? BrowserCatalog.MatchBrowser(browsers, state.Https)
                        : null;
                    Console.Write(Cli.FormatList(browsers, httpsDefault));
                    return 0;
                }

            case "--current":
                {
                    var state = services.AssociationReader.ReadCurrent();
                    Console.Write(Cli.FormatCurrent(state));
                    return 0;
                }

            case "--diagnose":
                {
                    var os = services.OsVersion.GetOsVersion();
                    var browsers = services.Catalog.Normalize(services.Source.Discover());
                    var state = services.AssociationReader.ReadCurrent();
                    Console.WriteLine($"OS build={os.Build} UBR={os.Ubr}");
                    Console.WriteLine("Registrations:");
                    foreach (var b in browsers)
                    {
                        Console.WriteLine($"{b.Id}\t{b.DisplayName}\t{b.PrimaryProgId}\t{b.ExecutablePath}");
                    }
                    Console.WriteLine("Current:");
                    Console.Write(Cli.FormatCurrent(state));
                    var linkMode = services.SettingsLauncher.IsPerAppOsSupported() ? "PerApp" : "Base";
                    Console.WriteLine($"SettingsLinkMode={linkMode}");
                    return 0;
                }

            case "--open-settings":
                {
                    bool ok = services.SettingsLauncher.LaunchDefaultApps();
                    if (!ok)
                    {
                        Console.Error.WriteLine("Could not open Settings. Open Settings -> Apps -> Default apps manually.");
                        return 1;
                    }
                    return 0;
                }

            case "--set-default":
                Console.Error.WriteLine("--set-default is not supported on Windows: there is no supported silent setter.");
                return 1;

            default:
                Console.Error.WriteLine($"Unknown argument: {cmd}");
                Console.Error.WriteLine(Cli.Usage);
                return 1;
        }
    }
}