# AGENTS.md — Windows app

Phase 0 (research spike and spec lock) is complete. **No implementation exists yet.** Read the
root `AGENTS.md`, `docs/product-spec.md`, and `docs/architecture.md` before changing anything.
The Windows section of the product spec is the behavior contract.

## Stack (decided)

- C# on **.NET 10 LTS**; all projects target `net10.0-windows10.0.19041.0` with
  `SupportedOSPlatformVersion` 10.0.17763.0. Only the app project sets `UseWindowsForms`.
- Minimum OS floor: Windows 10 1809 (build 17763); validation starts at Windows 10 22H2
  (best effort) and Windows 11 23H2+. Windows Server is out of scope.
- WinForms `NotifyIcon` + `ContextMenuStrip` + `TaskDialog`; no main window or taskbar button.
- No runtime NuGet dependency. MSTest and the test SDK (test-only) are the only package
  dependencies.
- x64 first; arm64 is optional later.

## Planned layout

```text
apps/windows/
├── Defaulty.sln
├── src/Defaulty.Core/       # no WinForms: models, discovery, state, catalog, settings URI
├── src/Defaulty/            # WinForms tray app and CLI entry point
├── tests/
│   ├── Defaulty.Core.Tests/
│   ├── Defaulty.Integration.Tests/
│   └── Defaulty.Safety.Tests/
├── packaging/
│   ├── app.manifest         # asInvoker, PerMonitorV2
│   ├── Defaulty.ico
│   └── publish.ps1
├── AGENTS.md
└── README.md
```

## Commands (planned; add with the first implementation)

Run from `apps/windows/`:

```powershell
dotnet build Defaulty.sln -c Release
dotnet test Defaulty.sln -c Release
dotnet publish src/Defaulty/Defaulty.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

From the repo root, add and use `make windows-build`, `make windows-test`, and
`make windows-verify`. Do not add targets that silently skip failures.

## Implementation rules

- **Discovery is read-only.** Enumerate `HKLM\SOFTWARE\RegisteredApplications` (64-bit and
  32-bit views) and `HKCU\SOFTWARE\RegisteredApplications`; follow each value to its
  `Capabilities` key in the same registry view; keep entries that claim `http` and/or `https`.
  Do not open `UserChoice` directly.
- **State is read-only.** Use `AssocQueryStringW` with
  `ASSOCF_IS_PROTOCOL | ASSOCF_APP_TO_APP` for `https` and `http`, requesting
  `ASSOCSTR_PROGID`, `ASSOCSTR_EXECUTABLE`, `ASSOCSTR_FRIENDLYAPPNAME`,
  `ASSOCSTR_APPICONREFERENCE`, and `ASSOCSTR_APPID`. Treat `https` as canonical and handle an
  HTTP/HTTPS mismatch without checking two items.
- **Change only through Windows.** Open `ms-settings:defaultapps` (per-app parameter when the
  Windows 11 build and `UBR` support it; base page otherwise). Never call
  `IAssocHandler::MakeDefault`, `IApplicationAssociationRegistration`, `SetUserFTA`, or any
  association setter. Never write to the registry, read `UserChoice` directly, send keystrokes,
  drive UI Automation, request elevation, or automate Settings. The `UBR` version read is
  read-only and used only for the Settings-link gate.
- **Never claim success.** Show a guidance dialog before opening Settings. Do not poll Settings.
  Re-read state the next time the menu opens; the checkmark is the only success signal.
- **Menu behavior.** Rebuild in the `Opening` handler. Exactly one checkmark when Windows reports
  a default; the checked item stays enabled. Include a conditional HTTP/HTTPS mismatch note, an
  always-available **Open Default apps…** action, and **Quit Defaulty**.
- **Single instance.** Use a per-session named mutex so a second launch does not add a second
  tray icon. No background daemons, timers, or login items.
- **No files, preferences, network, telemetry, or history.**
- **Localization and accessibility.** Resolve resource-backed names with `SHLoadIndirectString`;
  set tray tooltip/accessible text; keep every action keyboard-reachable. English strings are
  fine until localization is requested.

## CLI probes

Read-only modes (no tray icon, no single-instance mutex, results on stdout):

```powershell
Defaulty.exe --list
Defaulty.exe --current
Defaulty.exe --diagnose
Defaulty.exe --help
```

Read-only modes exit 0 even when no default can be resolved (print `none` or nothing). Unknown
arguments exit non-zero. `--open-settings` is a manual-only, user-visible action. There is
deliberately **no** `--set-default`; reject it with a non-zero exit code and change nothing.

## Testing rules

- Unit tests use fakes for discovery, state, and the Settings launcher.
- Integration tests are read-only and tolerant of different browser sets. They must never launch
  Settings or change a default.
- The static safety audit scans source and manifest for registry writes, association setters,
  direct `UserChoice` access, UI Automation/keystroke injection, network APIs, startup
  registration, and elevation. Keep it passing.
- Visual checks require the user. Ask them to run the Windows checklist in `docs/validation.md`.

## Packaging notes

- Release build is self-contained, single-file, `win-x64`; do not trim WinForms. Set
  `IncludeNativeLibrariesForSelfExtract=true`, otherwise the runtime's native libraries ship
  beside the executable. The host extracts them to `%TEMP%\.net` on first launch; that is
  expected runtime behavior, not app data.
- `app.manifest` must request `asInvoker` and PerMonitorV2.
- Authenticode signing is optional and uses a user-supplied certificate. Never commit
  certificates, private keys, or notarization material.
- Output goes to `artifacts/win-x64/` and stays untracked.

## Known edge cases

- A browser that registers only through an AppX manifest may be missing from
  `RegisteredApplications`; synthesize the current default and offer **Open Default apps…**.
- Skip stale registrations whose `Capabilities` key or executable is gone. If Windows still
  reports one as the current default, the synthetic item keeps the menu truthful.
- Per-app Settings links require Windows 11 21H2 build 22000.1817+, 22H2 build 22621.1555+, or
  23H2+. Because `Environment.OSVersion.Version.Revision` is 0 on .NET, read the `UBR` DWORD from
  `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` for the 21H2/22H2 gate; 23H2+ needs only the
  build. Fall back to `ms-settings:defaultapps` on older builds, an unreadable `UBR`, or launch
  failure.
- Windows 11 24H2+ varies the Default apps layout/search; instructions must name the browser and
  not rely on search.
- Icons and names may be resource-backed; fall back to the executable icon, then a generic globe.
- `NotifyIcon.Text` has a length limit; truncate gracefully.
- Windows has no standard global quit shortcut; use menu keyboard access.
