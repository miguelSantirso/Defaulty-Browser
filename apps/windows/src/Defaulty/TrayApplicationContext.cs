using System;
using System.Drawing;
using System.Windows.Forms;
using Defaulty.Core;

namespace Defaulty;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly CoreServices _services;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Image _fallbackImage;

    public TrayApplicationContext(CoreServices services)
    {
        _services = services;
        _menu = new ContextMenuStrip();
        _menu.Opening += OnMenuOpening;

        _fallbackImage = LoadAppIconBitmap();

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "Defaulty - No default browser detected",
            ContextMenuStrip = _menu,
            Visible = true,
        };

        // Left click opens the same menu.
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _menu.Show(Cursor.Position);
            }
        };

        RefreshTooltip();
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _menu.Items.Clear();

        BrowserCatalog.CurrentDefaultResolution resolution = new(null, null, false, false);
        bool hasError = false;

        try
        {
            var browsers = _services.Catalog.Normalize(_services.Source.Discover());
            var state = _services.AssociationReader.ReadCurrent();
            var menuBrowsers = BrowserCatalog.WithCurrentDefault(browsers, state, out resolution);

            foreach (var browser in menuBrowsers)
            {
                var item = new ToolStripMenuItem(browser.DisplayName)
                {
                    Image = LoadBrowserImage(browser) ?? _fallbackImage,
                    Checked = resolution.HasDefault && resolution.CheckedBrowser is not null && browser.Id == resolution.CheckedBrowser.Id,
                    Enabled = true,
                };

                Browser captured = browser;
                BrowserCatalog.CurrentDefaultResolution capturedResolution = resolution;
                item.Click += (_, _) => HandleBrowserSelected(captured, capturedResolution);
                _menu.Items.Add(item);
            }

            if (resolution.IsMismatch && resolution.HttpBrowser is not null)
            {
                var note = new ToolStripMenuItem($"HTTP default is {resolution.HttpBrowser.DisplayName}")
                {
                    Enabled = false,
                };
                _menu.Items.Add(note);
            }

            _menu.Items.Add(new ToolStripSeparator());

            var openDefaultApps = new ToolStripMenuItem("Open Default apps…");
            openDefaultApps.Click += (_, _) =>
            {
                if (!_services.SettingsLauncher.LaunchDefaultApps())
                {
                    try
                    {
                        TaskDialog.ShowDialog(new TaskDialogPage
                        {
                            Caption = "Defaulty",
                            Heading = "Couldn’t open Windows Settings",
                            Text = "Open Settings yourself, then go to Apps > Default apps to choose your default browser.",
                            Icon = TaskDialogIcon.Error,
                            Buttons = { TaskDialogButton.OK },
                            DefaultButton = TaskDialogButton.OK,
                        });
                    }
                    catch
                    {
                        MessageBox.Show(
                            "Could not open Settings. Open Settings -> Apps -> Default apps manually.",
                            "Defaulty",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            };
            _menu.Items.Add(openDefaultApps);

            var quit = new ToolStripMenuItem("Quit Defaulty");
            quit.Click += (_, _) => Application.Exit();
            _menu.Items.Add(quit);
        }
        catch (Exception ex)
        {
            hasError = true;
            var errorItem = new ToolStripMenuItem("Error loading browsers")
            {
                Enabled = false,
            };
            _menu.Items.Add(errorItem);
            try
            {
                TaskDialog.ShowDialog(new TaskDialogPage
                {
                    Caption = "Defaulty",
                    Heading = "Couldn’t read the browser list",
                    Text = $"{ex.Message}\n\nTry reopening the menu. If it keeps happening, check that Windows can list installed apps.",
                    Icon = TaskDialogIcon.Error,
                    Buttons = { TaskDialogButton.OK },
                    DefaultButton = TaskDialogButton.OK,
                });
            }
            catch
            {
                MessageBox.Show(
                    $"Could not read browser list: {ex.Message}",
                    "Defaulty",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        if (!hasError)
        {
            RefreshTooltip(resolution);
        }
    }

    private void HandleBrowserSelected(Browser browser, BrowserCatalog.CurrentDefaultResolution resolution)
    {
        // Already the checked default: do nothing.
        if (resolution.HasDefault && resolution.CheckedBrowser is not null && browser.Id == resolution.CheckedBrowser.Id)
        {
            return;
        }

        bool perApp = _services.SettingsLauncher.SupportsPerAppLink(browser);
        string step2 = perApp
            ? $"2. In Settings, click **Set default** for {browser.DisplayName}. If you see individual link types, set both **HTTP** and **HTTPS** to {browser.DisplayName}."
            : $"2. On the Default apps page, choose **{browser.DisplayName}** as your web browser (on Windows 10, look under **Web browser**; on Windows 11, search for {browser.DisplayName} if needed).";

        var page = new TaskDialogPage
        {
            Caption = "Defaulty",
            Heading = $"Make {browser.DisplayName} your default browser",
            Text =
                "Windows needs you to confirm this in Settings. Defaulty can’t change it for you.\n\n" +
                "1. Click **Open Windows Settings** below.\n" +
                step2 + "\n" +
                "3. Reopen this menu in Defaulty to see the checkmark.",
            Footnote = new TaskDialogFootnote($"If {browser.DisplayName} isn’t shown, use the search box on the Default apps page."),
            Icon = TaskDialogIcon.Information,
            AllowCancel = true,
        };

        var openButton = new TaskDialogCommandLinkButton("Open Windows Settings");
        var notNowButton = new TaskDialogCommandLinkButton("Not now");
        page.Buttons.Add(openButton);
        page.Buttons.Add(notNowButton);
        page.DefaultButton = openButton;

        TaskDialogButton clicked;
        try
        {
            clicked = TaskDialog.ShowDialog(page);
        }
        catch
        {
            return;
        }

        if (clicked == openButton)
        {
            bool ok = _services.SettingsLauncher.Launch(browser);
            if (!ok)
            {
                ShowSettingsLaunchError(browser.DisplayName);
            }
        }
    }

    private static void ShowSettingsLaunchError(string browserName)
    {
        try
        {
            var errorPage = new TaskDialogPage
            {
                Caption = "Defaulty",
                Heading = "Couldn’t open Windows Settings",
                Text =
                    $"Open Settings yourself, then go to Apps > Default apps and choose {browserName}.\n\n" +
                    "After you confirm in Windows, reopen the Defaulty menu to see the checkmark.",
                Icon = TaskDialogIcon.Error,
                Buttons = { TaskDialogButton.OK },
                DefaultButton = TaskDialogButton.OK,
            };
            TaskDialog.ShowDialog(errorPage);
        }
        catch
        {
            MessageBox.Show(
                $"Could not open Settings. Open Settings -> Apps -> Default apps and choose {browserName}.",
                "Defaulty",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void RefreshTooltip(BrowserCatalog.CurrentDefaultResolution? resolution = null)
    {
        string text;
        if (resolution is null)
        {
            try
            {
                var browsers = _services.Catalog.Normalize(_services.Source.Discover());
                var state = _services.AssociationReader.ReadCurrent();
                var res = BrowserCatalog.ResolveCurrent(browsers, state);
                text = res.HasDefault && res.CheckedBrowser is not null
                    ? $"Defaulty - {res.CheckedBrowser.DisplayName}"
                    : "Defaulty - No default browser detected";
            }
            catch
            {
                text = "Defaulty";
            }
        }
        else
        {
            text = resolution.HasDefault && resolution.CheckedBrowser is not null
                ? $"Defaulty - {resolution.CheckedBrowser.DisplayName}"
                : "Defaulty - No default browser detected";
        }

        if (text.Length > 63)
        {
            text = text.Substring(0, 60) + "...";
        }
        _notifyIcon.Text = text;
    }

    private static Icon LoadAppIcon()
    {
        try
        {
            using var extracted = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (extracted is not null)
            {
                return (Icon)extracted.Clone();
            }
        }
        catch
        {
        }
        return (Icon)SystemIcons.Application.Clone();
    }

    private static Image LoadAppIconBitmap()
    {
        try
        {
            using var icon = LoadAppIcon();
            return icon.ToBitmap();
        }
        catch
        {
            return new Bitmap(16, 16);
        }
    }

    private static Image? LoadBrowserImage(Browser browser)
    {
        try
        {
            string? path = null;
            if (!string.IsNullOrWhiteSpace(browser.IconReference) && System.IO.File.Exists(browser.IconReference))
            {
                path = browser.IconReference;
            }
            else if (!string.IsNullOrWhiteSpace(browser.ExecutablePath) && System.IO.File.Exists(browser.ExecutablePath))
            {
                path = browser.ExecutablePath;
            }

            if (path is not null)
            {
                using var icon = Icon.ExtractAssociatedIcon(path);
                if (icon is not null)
                {
                    return icon.ToBitmap();
                }
            }
        }
        catch
        {
        }
        return null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
            _fallbackImage.Dispose();
        }
        base.Dispose(disposing);
    }
}
