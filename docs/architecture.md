# Architecture

Defaulty is a small cross-platform utility with **one shared product contract** and
**independent native implementations per platform**. Keep platform code inside its app
directory; share behavior through documentation, not through a runtime framework.

## Repository boundaries

| Path | Purpose |
| --- | --- |
| `apps/macos/` | Swift 6 + AppKit implementation, packaging, and tests |
| `apps/windows/` | C# / .NET 10 LTS tray implementation (designed; no code yet) |
| `docs/` | Product spec, this architecture document, validation procedures |
| `AGENTS.md` | Repo-wide rules for coding agents |
| `Makefile` | Thin, namespaced orchestration (`make macos-*`) |

No shared runtime library exists yet, and none is needed while each platform is a
self-contained native app. If shared assets or pure domain logic appear later, add a
top-level `shared/` directory and document its ownership here.

## Shared domain model

Every implementation has the same four responsibilities:

1. **Discovery** — enumerate installed web handlers and their display names/icons.
2. **State** — read the current system default web handler.
3. **Change** — request that a chosen handler become the default, using supported APIs
   and any platform-required user confirmation.
4. **Presentation** — a status/tray icon and native menu with a checked current item.

The platform layer is allowed to differ in mechanics (especially around consent) but must
preserve the shared UX contract in `docs/product-spec.md`.

## macOS implementation

| Responsibility | Implementation |
| --- | --- |
| Domain value | `apps/macos/Sources/DefaultyCore/Browser.swift` |
| Discovery | `apps/macos/Sources/DefaultyCore/LaunchServicesBrowserDiscovery.swift` |
| Normalization | `apps/macos/Sources/DefaultyCore/BrowserCatalog.swift` |
| Read/change default | `apps/macos/Sources/DefaultyCore/DefaultBrowserService.swift` |
| Menu bar UI | `apps/macos/Sources/Defaulty/StatusMenuController.swift` |
| Lifecycle | `apps/macos/Sources/Defaulty/AppDelegate.swift`, `apps/macos/Sources/Defaulty/main.swift` |
| CLI probes | `apps/macos/Sources/Defaulty/main.swift` (`--list`, `--current`, `--set-default`) |
| Packaging | `apps/macos/Scripts/build-app.sh`, `apps/macos/Resources/Info.plist`, `apps/macos/Resources/AppIcon.icns` |
| Tests | `apps/macos/Tests/DefaultyCoreTests/` |

### Data flow

1. `StatusMenuController.menuNeedsUpdate` calls `BrowserCatalog` (Launch Services
   discovery) and `DefaultBrowserService.currentDefaultBrowserBundleIdentifier()`.
2. The controller builds a native menu with app icons and a checkmark on the current default.
3. Selecting an item awaits `DefaultBrowserService.setDefaultBrowser`, which sets `https`
   then `http` through `NSWorkspace`.
4. On success the menu is rebuilt; on failure a native alert is shown and state is refreshed.

### Design decisions

- **SwiftPM, not an Xcode project** — reproducible builds and simple agent/CI workflows;
  the build script assembles a real `.app` bundle.
- **AppKit, not SwiftUI/MenuBarExtra** — precise control over `NSStatusItem`/`NSMenu`
  checkmarks, app icons, and actions on the minimum supported OS.
- **`LSUIElement` agent** — no Dock icon or app switcher entry, matching menu bar utility norms.
- **`NSWorkspace` default-application APIs** — supported API; no Launch Services plist editing
  and no deprecated `LSSetDefaultHandlerForURLScheme`.
- **Ad-hoc signing** — appropriate for a locally built personal utility; not notarized.
- **No sandbox** — the app is a local tool that must request default-handler changes.

### Constraints

- Default browser changes may require user consent depending on macOS version/policy.
- Launch Services can report handlers from alternate volumes; resolve to the canonical app
  URL and deduplicate by bundle identifier.
- `NSWorkspace` completion handlers are delivered on the main run loop; never block the main
  thread while awaiting them.
- Minimal deployment target is macOS 13.

## Windows implementation (implemented)

Target: Windows 10 22H2 (best effort) and Windows 11 21H2 or later, x64. Minimum OS is Windows
10 1809 (build 17763), declared with `SupportedOSPlatformVersion` 10.0.17763.0. The
implementation is C# on .NET 10 LTS (`net10.0-windows10.0.19041.0`, `UseWindowsForms`). Windows
Server is out of scope. No runtime NuGet dependency; MSTest/test SDK are test-only.

Build 17763 is the lowest TFM-compatible API floor, not a validation target: product validation
starts at Windows 10 22H2, and older Windows 10 builds are untested.

These decisions were checked against Microsoft Learn in November 2026
(`SHAssocEnumHandlers`/`IAssocHandler`, `AssocQueryString`/`ASSOCF`, Default Programs,
`RegisteredApplications`, and the `ms-settings:defaultapps` deep-link contract).

| Responsibility | Implementation |
| --- | --- |
| Domain value | `apps/windows/src/Defaulty.Core/Browser.cs` |
| Discovery | `apps/windows/src/Defaulty.Core/RegisteredApplicationSource.cs`, `CapabilitiesReader.cs` |
| Normalization | `apps/windows/src/Defaulty.Core/BrowserCatalog.cs` (filter, deduplicate, sort) |
| State | `apps/windows/src/Defaulty.Core/DefaultAssociationReader.cs` (`AssocQueryStringW`) |
| Change request | `apps/windows/src/Defaulty.Core/SettingsLauncher.cs` (`ms-settings`, build + UBR gate) |
| Presentation | `apps/windows/src/Defaulty/TrayApplicationContext.cs`, `TrayMenuBuilder.cs` |
| Lifecycle | `apps/windows/src/Defaulty/Program.cs`, single-instance mutex |
| CLI probes | `apps/windows/src/Defaulty/Program.cs` (`--list`, `--current`, `--diagnose`, `--open-settings`) |
| Packaging | `apps/windows/packaging/publish.ps1`, `app.manifest`, `Defaulty.ico` |
| Tests | `apps/windows/tests/Defaulty.Core.Tests/`, `Defaulty.Integration.Tests/`, `Defaulty.Safety.Tests/` |

### Data flow

1. The tray menu's `Opening` handler refreshes state synchronously (local registry and shell
   reads only).
2. `RegisteredApplicationSource` enumerates `RegisteredApplications` under HKLM (64-bit and
   32-bit views) and HKCU, reads each `Capabilities` key in the same registry view that supplied
   the registration, and keeps entries that claim `http` and/or `https`.
3. `BrowserCatalog` drops `Hidden` and stale entries, resolves display names/icons, merges
   duplicate registrations by primary ProgId or canonical executable path, prefers a valid
   per-user registration, and sorts.
4. `DefaultAssociationReader` queries `https` and then `http` with `AssocQueryStringW`
   (`ASSOCF_IS_PROTOCOL | ASSOCF_APP_TO_APP`) for ProgId, executable, friendly name, icon
   reference, and AUMID. The catalog matches them to a browser; `https` is canonical and a
   mismatch is reported separately.
5. `TrayMenuBuilder` builds the context menu with icons, one checkmark, the mismatch note,
   **Open Default apps…**, and **Quit Defaulty**.
6. Selecting a non-current browser shows a native `TaskDialog`. **Open Settings** calls
   `SettingsLauncher`; nothing is written. The next menu open repeats steps 1–5 and shows the
   actual result.

### Design decisions

- **.NET 10 LTS** — the current LTS (supported through November 2028; .NET 8 is already out of
  support as of November 2026). C# keeps the code small and testable without a cross-platform UI
  framework.
- **WinForms, not WinUI 3/WPF/raw Win32** — `NotifyIcon`, `ContextMenuStrip`, and `TaskDialog`
  are built into the Windows Desktop runtime, need no third-party package, and provide native
  icons, checkmarks, keyboard access, and system color/dark mode in .NET 10. WinUI 3 adds the
  Windows App SDK and has no built-in tray icon; WPF is heavier; raw `Shell_NotifyIcon` would
  add significant menu, accessibility, and DPI code for Phase 1.
- **RegisteredApplications/Capabilities discovery** — this is the documented Default Programs
  registration schema and is the data the Settings deep link expects. It gives display names
  (resolving resource strings), protocol ProgIds, `Hidden`, and the exact registration name
  needed for per-app deep links. The deprecated association *setter* APIs are not used.
- **`AssocQueryString`, not `UserChoice`** — `AssocQueryStringW` with
  `ASSOCF_IS_PROTOCOL | ASSOCF_APP_TO_APP` is the supported way to read the effective
  current-user association; it avoids parsing protected state and keeps the app honest about
  per-user overrides.
- **Per-app Settings deep link** — documented by Microsoft for Windows 11 21H2 build
  22000.1817+, 22H2 build 22621.1555+, and 23H2+. The app uses
  `registeredAppMachine`/`registeredAppUser` with the `RegisteredApplications` value name and
  falls back to `ms-settings:defaultapps` on older builds or launch failure. Because
  `Environment.OSVersion.Version.Revision` is 0 on .NET, the 21H2/22H2 gate reads the update
  revision (`UBR`) from the read-only `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` key;
  23H2+ needs only the build number, and an unreadable `UBR` on 21H2/22H2 means the base page.
- **Guidance dialog before Settings** — the user sees the exact steps and explicitly chooses
  **Open Settings**. This avoids stealing focus and avoids claiming a change that Windows has
  not applied.
- **No `IAssocHandler::MakeDefault`, `IApplicationAssociationRegistration`, registry writes,
  UI Automation, or keystroke injection** — these either bypass Windows' protections or
  automate consent, both prohibited by the repository rules.
- **Self-contained, single-file release** — removes the .NET Desktop Runtime prerequisite.
  WinForms trimming is not supported, so the executable is larger than a framework-dependent
  build; runtime footprint stays small. Native runtime libraries are bundled for a single output
  file and extracted by the host to `%TEMP%\.net` on first launch.
- **MSTest, test-only** — .NET has no built-in test framework. The test SDK/framework packages
  are not shipped with the app; the runtime app stays package-free.
- **Single-instance mutex, no global hotkey** — one tray icon; Windows tray utilities use menu
  keyboard access rather than a global quit shortcut.
- **Headless CLI probes** — `--list`, `--current`, `--diagnose`, and `--help` write to standard
  output and exit without creating a tray icon or taking the single-instance mutex, so they work
  while the tray app is running. Read-only modes exit 0 even when no default is resolvable;
  `--set-default` exits non-zero and changes nothing.
- **Core targets Windows** — `Defaulty.Core` also uses the Windows TFM
  (`net10.0-windows10.0.19041.0`, no WinForms). Registry and shell APIs are Windows-only and
  annotated as such; a platform-neutral target would only add compatibility warnings for no
  benefit.

### Discovery alternatives considered

- **`SHAssocEnumHandlers("http"/"https")`** — a documented shell enumeration API, but its
  documented input is a file-name extension, not a URL protocol. Protocol use works in practice
  and appears in community samples, but it is not a documented contract. Not the primary source;
  revisit only if `RegisteredApplications` misses real browsers, with a spec update first.
- **`HKLM/HKCU\SOFTWARE\Clients\StartMenuInternet`** — legacy client-type registration. It does
  not directly expose per-scheme capabilities or the registration name the Settings deep link
  needs.
- **AppX `PackageManager` / `Get-StartApps`** — packaged apps only, heavier, and it does not
  provide classic registration names or per-scheme ProgIds. Edge is also classically registered.
- **Direct `UserChoice` reads** — unnecessary and against this design's supported-API rule;
  `AssocQueryString` gives the effective result. The app never opens the key directly.

### Constraints and risks

- Microsoft's Default Programs *API set* is deprecated for Windows 8+ and the older Default
  Programs topic says it does not apply to Windows 10. Defaulty relies only on the
  `RegisteredApplications`/`Capabilities` registration schema, which the documented
  `ms-settings:defaultapps` contract still reads and which Windows Settings still populates. No
  deprecated association API is called.
- The only non-association registry read is the `UBR` DWORD used for the Settings build gate.
  It is read-only and never written. If access is denied, the launcher uses the base page.
- A browser that registers only through an AppX manifest or only through raw ProgIDs may be
  missing from discovery. The synthetic current-default entry and the always-available **Open
  Default apps…** action keep the app truthful about the current default; Phase 1 must validate
  Chrome, Edge, Firefox, Brave, Opera, and Vivaldi on Windows 10 22H2 and Windows 11
  23H2/24H2/25H2.
- `ASSOCSTR_PROGID` was introduced in Windows 10. If it is unavailable, the reader falls back to
  executable/friendly-name matching; if all reads fail, the menu reports the failure instead of
  guessing.
- Per-app Settings links depend on the Windows 11 build and cumulative update. The build gate is
  conservative; an ignored query parameter still lands on the Default apps page, and the dialog
  instructions remain correct. Links are used only for native-view HKLM/HKCU registrations;
  32-bit-only entries fall back to the base page. `ASSOCSTR_APPID` is captured for diagnostics and
  a future AppX discovery source could use `registeredAUMID`; v1 uses machine/user names.
- Windows 11 24H2 and later vary the Default apps layout/search. The instructions name the
  browser and never rely on search results.
- Display names and icons can be resource-backed (`@dll,-id`). Use `SHLoadIndirectString` and
  `ExtractIconEx`/`DefaultIcon`, with a generic globe fallback.
- `NotifyIcon.Text` has a platform length limit; truncate tooltips gracefully.
- WinForms dark mode is new in .NET 10 and has known gaps, especially in `TaskDialog`. Verify
  visually; a system-light dialog is acceptable if Windows renders it that way.
- A browser that claims only one scheme is still listed. The other scheme remains unchanged and
  the checkmark/mismatch note tells the truth; the app never claims both were set.
- Unsigned local builds can trigger SmartScreen. Authenticode signing is optional, uses a
  user-supplied certificate, and never commits signing material.
- Automated tests must remain read-only and must never launch Settings or change a default.
- Windows 10 consumer support ended in October 2025, and .NET 10's supported Windows 10 list
  covers only LTSC/Enterprise editions. Consumer Windows 10 22H2 therefore remains best effort:
  the self-contained app is expected to run, but the OS itself is unsupported.

### Packaging outline

```text
apps/windows/
├── src/Defaulty.Core/          # net10.0-windows10.0.19041.0, no WinForms
├── src/Defaulty/               # same TFM: WinForms tray + CLI
├── tests/…                     # unit, read-only integration, static safety audit
├── packaging/
│   ├── app.manifest            # asInvoker, PerMonitorV2, supported OS
│   ├── Defaulty.ico
│   └── publish.ps1             # win-x64 self-contained single-file release
├── Defaulty.sln
├── AGENTS.md
└── README.md
```

Release command shape (single file, no trimming):

```powershell
dotnet publish src/Defaulty/Defaulty.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The artifact is `apps/windows/artifacts/win-x64/Defaulty.exe`, a portable executable; no
installer, MSIX identity, startup entry, or service is planned for v1. Build outputs stay
untracked. `IncludeNativeLibrariesForSelfExtract` is required for a genuinely single output file:
without it, the runtime's native libraries sit beside the executable. The trade-off is that the
host extracts those libraries to `%TEMP%\.net` on first launch. That is runtime behavior, not
Defaulty app data. `EnableCompressionInSingleFile` is an optional size lever that costs startup
time and is off by default.

### References

- [SHAssocEnumHandlers](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nf-shobjidl_core-shassocenumhandlers)
- [IAssocHandler](https://learn.microsoft.com/windows/win32/api/shobjidl_core/nn-shobjidl_core-iassochandler)
- [AssocQueryStringW](https://learn.microsoft.com/windows/win32/api/shlwapi/nf-shlwapi-assocquerystringw)
- [ASSOCF flags](https://learn.microsoft.com/windows/win32/shell/assocf_str)
- [Default Programs / RegisteredApplications](https://learn.microsoft.com/windows/win32/shell/default-programs)
- [Launch the Default Apps settings page](https://learn.microsoft.com/windows/apps/develop/launch/launch-default-apps-settings)

### Hard constraints

- Windows protects default-app associations with a per-user `UserChoice` hash.
- There is no supported, silent API for third-party apps to set the default browser.
- Do not use undocumented hash bypasses, registry patching, association setters, or automation
  of consent dialogs.
- The shared UX contract adapts: Defaulty guides the user through Settings, then re-reads state.

## Adding a platform

1. Update `docs/product-spec.md` with platform behavior and acceptance criteria.
2. Add `apps/<platform>/` with its own `AGENTS.md` and `README.md`.
3. Add isolated build/test/package commands and wire them into the root `Makefile`.
4. Add CI for that platform.
5. Add validation steps to `docs/validation.md`.
6. Verify the shared acceptance criteria and document any OS-mandated differences.
