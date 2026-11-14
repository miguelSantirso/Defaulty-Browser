# Defaulty for Windows

**Status: Implemented (core, tray UI, CLI, packaging, tests, safety audit). Manual validation and CI hardening remain.**
Phase 0 (research spike and spec lock) is complete, and the testable core logic in
`src/Defaulty.Core` (discovery, state, catalog, settings URI) is implemented.

See the shared [product spec](../../docs/product-spec.md) and
[architecture](../../docs/architecture.md) for behavior and design. Implementation rules for
agents are in [AGENTS.md](AGENTS.md).

## Why it is not a direct port

Windows 10/11 protect default-app associations. The default browser is stored in a per-user
`UserChoice` entry that includes a hash, and Microsoft does not provide a supported API for a
third-party app to silently set the default browser. Defaulty must not bypass that protection.

The Windows UX is therefore **user-mediated**: Defaulty lists browsers, marks the current
default, and guides the user through the official Windows **Settings → Default apps** flow,
then re-reads the result. It never writes to the registry, never opens `UserChoice` directly,
and never automates Settings.

## Chosen approach

- **Discovery (read-only):** `HKLM`/`HKCU\SOFTWARE\RegisteredApplications` →
  `Capabilities\UrlAssociations\http|https`. Display names are resolved with
  `SHLoadIndirectString`; icons come from the ProgId `DefaultIcon` or the executable.
- **Current default (read-only):** `AssocQueryStringW` with
  `ASSOCF_IS_PROTOCOL | ASSOCF_APP_TO_APP` for `https` and `http`. `https` is canonical; an
  HTTP/HTTPS mismatch is shown as a note, never as a second checkmark.
- **Change:** selecting a browser shows a native guidance dialog. **Open Settings** launches
  `ms-settings:defaultapps` with the browser's registration name on supported Windows 11 builds
  (base page on Windows 10/older builds). The user confirms in Settings; Defaulty re-reads state
  the next time the menu opens and never claims success early.
- **Presentation:** WinForms `NotifyIcon` + `ContextMenuStrip` + `TaskDialog`, no main window,
  one tray icon, no background work.

## Structure

```text
apps/windows/
├── Defaulty.sln
├── src/Defaulty.Core/       # testable domain logic, discovery, state, settings URI
├── src/Defaulty/            # WinForms tray app and CLI probes
├── tests/                   # unit, read-only integration, static safety audit
├── packaging/               # app.manifest, icon, publish script
├── AGENTS.md
└── README.md
```

## Commands

Run from `apps/windows/`:
```powershell
dotnet build Defaulty.sln -c Release
dotnet test Defaulty.sln -c Release
dotnet publish src/Defaulty/Defaulty.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Root targets: `make windows-build`, `make windows-test`, `make windows-publish`,
`make windows-verify`.

## Usage

1. Launch Defaulty. A globe appears in the notification area.
2. Open the menu. The current default has a checkmark.
3. Choose another browser. Read the short instructions and choose **Open Settings**.
4. In Settings, use **Set default** (Windows 11) or the **Web browser** dropdown (Windows 10)
   to choose the browser.
5. Return to Defaulty and reopen the menu. The checkmark shows the actual current default.

## CLI probes (read-only)

```powershell
Defaulty.exe --list
Defaulty.exe --current
Defaulty.exe --diagnose
Defaulty.exe --help
```

`--open-settings` is manual-only. Read-only probes run headless (no tray icon or mutex) and exit
0 even when no default can be read. There is no `--set-default` on Windows by design.

## Notes and limitations

- Windows 11 21H2/22H2 need the 2023-04 cumulative update for per-app Default apps links;
  older builds fall back to the base page.
- Windows 11 24H2+ varies the Default apps layout/search. Instructions name the browser and do
  not depend on search.
- A browser registered only through an AppX manifest may be missing from discovery. The current
  default is still shown, and **Open Default apps…** is always available.
- Release builds are self-contained and therefore larger than a framework-dependent build.
  WinForms trimming is not supported.
- The single-file host extracts bundled .NET runtime libraries to `%TEMP%\.net` on first launch;
  that is runtime behavior, not app data.
- Local builds can trigger SmartScreen; Authenticode signing is optional and uses a
  user-supplied certificate.
- Windows has no standard global quit shortcut; use the menu and keyboard navigation.
- No files, preferences, startup entries, network access, or telemetry.

## Validation

See the Windows checklist in [docs/validation.md](../../docs/validation.md). It includes an
explicit Process Monitor check that Defaulty performs no registry writes and never bypasses
Windows' default-app protections. Visual checks require a human.
