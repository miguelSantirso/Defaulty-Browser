using System;
using System.Threading;
using System.Windows.Forms;
using Defaulty.Core;

namespace Defaulty;

internal static class Program
{
    private const string MutexName = @"Local\Defaulty";

    [STAThread]
    private static void Main(string[] args)
    {
        // Headless CLI modes: no tray icon, no mutex, no UI.
        if (args.Length > 0)
        {
            int code = CliRunner.Run(args);
            Environment.Exit(code);
            return;
        }

        // Single-instance guard.
        bool createdNew;
        Mutex mutex;
        try
        {
            mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);
        }
        catch
        {
            return;
        }

        if (!createdNew)
        {
            mutex.Dispose();
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();
            var services = new CoreServices();
            Application.Run(new TrayApplicationContext(services));
        }
        finally
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch
            {
            }
            mutex.Dispose();
        }
    }
}