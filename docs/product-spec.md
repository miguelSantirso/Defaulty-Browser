# Defaulty — product specification

Status: **macOS v1.0.0 implemented. Windows tray app implemented (core, tray UI, CLI, packaging, tests).**
This document is the source of truth for behavior. Change it before changing behavior.

## Vision

Defaulty is a tiny utility that lets you switch the operating system's default web
browser from the menu bar (macOS) or notification area (Windows). One icon, one short
menu, no settings window, no account, no telemetry.

## Goals

- Make changing the default browser a short, obvious operation.
- Always show the truth: the menu reflects the current system default every time it opens.
- Use only supported, user-respecting platform APIs.
- Stay small: a single agent app with no main window, background daemons, or preferences.
- Share one product contract across platforms while keeping implementations isolated.

## Non-goals

- Browser profile management, per-site routing, or URL rewriting.
- Changing defaults for other app categories (mail, calendar, files).
- Bundling browsers, updaters, or telemetry.
- Silent changes on platforms that require the user to confirm (notably Windows).

## Shared UX contract

1. **Agent app** — no Dock icon (macOS) or taskbar window (Windows); only a status/tray icon.
2. **Status icon** — native-looking, template/monochrome where the platform expects it, with
   an accessible label and a tooltip naming the current default browser.
3. **Menu** — lists installed apps that the OS considers capable of opening web links,
   sorted in the platform's natural order.
4. **Current selection** — the current default is checked and remains enabled.
5. **Changing** — choosing a browser makes it the default for web links. If the OS requires
   confirmation, the app explains what to do instead of bypassing the OS. On Windows this means
   opening the OS flow; the app never claims success before the change is confirmed and state is
   re-read.
6. **Fresh state** — the menu re-reads installed browsers and the current default every
   time it opens; changes made elsewhere are reflected.
7. **Errors** — failures are surfaced in a native alert with a recovery hint.
8. **Quit** — an obvious quit command with the platform-standard keyboard shortcut where the
   platform defines one.
9. **Privacy** — no network access, analytics, preferences, or stored history.
10. **Tiny footprint** — launch quickly, use negligible memory, and leave no background work.

### Acceptance criteria

- Launching the app shows exactly one status/tray icon and no main window.
- Opening the menu shows every installed web handler once, with app icons and names.
- The current default has a checkmark; exactly one item is checked when the OS reports a default.
- Choosing a different browser either updates the system default (macOS) or opens the
  OS-mediated confirmation flow (Windows); the checkmark reflects the OS state afterward.
- Changing the default outside the app is reflected the next time the menu opens.
- A failed change produces a native, actionable error and leaves the menu consistent.
- Quit works from the menu and, where the platform defines one, the standard shortcut.
- No app data files, preferences, or login items are created.

## Platform notes

### macOS (implemented)

- **Discovery:** union of apps returned by `NSWorkspace.urlsForApplications(toOpen:)` for
  `http` and `https`, canonicalized, deduplicated, sorted by localized name.
- **Current default:** resolved with `NSWorkspace.urlForApplication(toOpen:)` using an
  `https` URL.
- **Switching:** `NSWorkspace.setDefaultApplication(at:toOpenURLsWithScheme:)` for `https`
  then `http`. macOS may present a consent prompt depending on OS version and policy.
- **Presentation:** `NSStatusItem` + native `NSMenu` in an `LSUIElement` AppKit agent.
- **Packaging:** SwiftPM executable assembled into `Defaulty.app`, ad-hoc signed.
- See `apps/macos/README.md` for commands and `apps/macos/AGENTS.md` for implementation rules.

### Windows (implemented)

Supported targets: Windows 10 22H2 (best effort; Microsoft consumer support ended in October
2025) and Windows 11 21H2 and later, x64. Primary validation is on Windows 11. Windows Server
is out of scope.

Windows default-app associations are user-controlled: Windows 10/11 protect browser
associations with a per-user `UserChoice` hash, and as of this design (November 2026) Microsoft
does not provide a supported silent API for a third-party app to set the default browser.
Defaulty must not bypass this. It therefore never writes to the registry, never opens
`UserChoice` or its hash directly, and never automates or bypasses Settings.

#### Discovery

Defaulty reads the Default Programs registration schema that Windows Settings uses, read-only:

- Enumerate `HKLM\SOFTWARE\RegisteredApplications` (64-bit and 32-bit views) and
  `HKCU\SOFTWARE\RegisteredApplications`. Each value name is the app's registration name and
  each value is the path to its `Capabilities` key.
- Keep an entry only when its `Capabilities\UrlAssociations` claims `http` and/or `https`.
  Read `ApplicationName` (resolve resource-backed `@…` strings with `SHLoadIndirectString`; if
  empty or unresolvable, use the executable base name, then the registration name), `Hidden`,
  and the protocol ProgIds.
- Exclude `Hidden = 1` entries. Resolve the icon from `HKCR\<ProgId>\DefaultIcon`, then the
  executable icon (resolved from `HKCR\<ProgId>\shell\open\command`), then a generic globe.
- Use the registration name plus hive (`machine:` / `user:`) as the stable identifier.
  Deduplicate registrations that resolve to the same browser (same primary ProgId or canonical
  executable path), preferring a valid per-user registration for the Settings link.
- Skip stale registrations whose `Capabilities` key or executable no longer exists. If Windows
  still reports a stale app as the current default, the synthetic entry below shows the truth.
- Sort by display name using the current culture, case-insensitively; break ties by identifier.
- If the current default is not in the registered list, add a synthetic checked entry built from
  `AssocQueryString` (`ASSOCSTR_FRIENDLYAPPNAME`, `ASSOCSTR_APPICONREFERENCE`) so the menu
  remains truthful.

Windows 11 24H2 and later vary the Default apps layout and search behavior. The app must always
name the target browser and must not depend on Settings search behavior.

#### Current default

- Read `https` and `http` with `AssocQueryStringW` using
  `ASSOCF_IS_PROTOCOL | ASSOCF_APP_TO_APP`, requesting `ASSOCSTR_PROGID`,
  `ASSOCSTR_EXECUTABLE`, `ASSOCSTR_FRIENDLYAPPNAME`, `ASSOCSTR_APPICONREFERENCE`, and
  `ASSOCSTR_APPID` (diagnostics; reserved for a future packaged-app discovery source).
- `https` is the canonical default. If `https` cannot be resolved, fall back to `http`; if both
  fail, report "No default browser detected" rather than inventing an answer.
- Match the current ProgId to a registered browser. If that fails, compare canonical executable
  paths; if that also fails, show the synthetic entry described above. Never open `UserChoice`
  directly.
- Compare resolved *browser identity*, not raw ProgIds, to detect an HTTP/HTTPS mismatch because
  one browser may use different ProgIds per scheme. When the schemes resolve to different
  browsers, check the `https` browser and show one disabled note naming the `http` browser.
  Exactly one item remains checked.

#### Changing (user-mediated)

1. The user chooses a browser from the tray menu.
2. If it is already the checked default, the menu closes without changing anything.
3. Otherwise the app shows a native guidance dialog naming the browser, with **Open Settings**
   and **Cancel**. Cancel changes nothing.
4. **Open Settings** launches the closest supported Settings page:
   - Windows 11 21H2 build 22000.1817+, 22H2 build 22621.1555+, or 23H2 and later:
     `ms-settings:defaultapps?registeredAppMachine=<escaped registration name>` for an HKLM
     entry, or `registeredAppUser=<escaped registration name>` for an HKCU entry. The value is
     the `RegisteredApplications` value name, not the display name.
   - The build gate uses the OS build; for 21H2 and 22H2 it also reads the `UBR` value under
     `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` (read-only). 23H2 and later need no
     `UBR` check; if `UBR` cannot be read on 21H2/22H2, use the base page.
   - Windows 10 or an older Windows 11 build: `ms-settings:defaultapps`.
   - If the per-app URI fails, fall back to `ms-settings:defaultapps`.
   - Per-app links are used only for registrations found in the native 64-bit HKLM view or the
     HKCU view; entries found only in the 32-bit view use the base page.
5. The dialog tells the user what to do:
   - Windows 11: select **Set default** for the named browser; if Windows shows individual link
     types, set both HTTP and HTTPS.
   - Windows 10: under **Web browser**, choose the named browser.
   - If the per-app page does not open, search for the browser name on the Default apps page.
   - Return to Defaulty and reopen the tray menu to confirm.
6. The app does not wait for, poll, or automate Settings. It never shows a success message. The
   next menu open re-reads both schemes, and the checkmark and tooltip show the actual result.
7. If Settings cannot be launched, a native error names the manual path:
   **Settings → Apps → Default apps**, and the menu remains consistent.

The app never calls `IAssocHandler::MakeDefault`, `IApplicationAssociationRegistration`, or any
other association setter. It does not send keystrokes, drive UI Automation, request elevation,
or write registry values.

#### Presentation

- WinForms `NotifyIcon` plus a `ContextMenuStrip`; no main window or taskbar button.
  A single-instance guard prevents a second tray icon.
- Globe tray icon, tooltip/accessible text naming the current default (truncated to the platform
  limit), and **No default browser detected** when no default can be read.
- Menu: browsers with icons, one checkmark on the current default, a conditional HTTP/HTTPS
  mismatch note, an always-available **Open Default apps…** action, and **Quit Defaulty**.
- Left-click and right-click open the same menu. Keyboard access uses Shift+F10 or the Menu key,
  arrow keys, and Enter. Windows has no standard global quit shortcut, so the shared contract's
  "platform-standard shortcut" is satisfied by menu keyboard access rather than a global hotkey.

#### Diagnostics (CLI)

Read-only probe modes run headless: no tray icon, no single-instance mutex, and results on
standard output. They are safe to run while the tray app is running.

- `--list` prints each browser once and prefixes the canonical `https` default with `* `. It
  exits 0, printing nothing when no browser is registered.
- `--current` prints the resolved `https` and `http` associations, using `none` for a scheme
  that cannot be resolved, and exits 0 even when no default is readable.
- `--diagnose` prints the OS build and UBR, registrations, current state, and Settings-link mode.
  It exits 0 unless the probe itself fails.
- `--help` prints usage and exits 0. Unknown arguments exit non-zero.
- `--set-default` is deliberately unsupported: it exits non-zero and changes nothing, because
  Windows has no supported silent setter.
- `--open-settings` opens the Default apps page. It is a user-visible action for manual use only
  and is never invoked by automated tests.

#### Windows acceptance criteria

- Launching shows exactly one globe icon in the notification area, no main window, and no
  taskbar button. A second launch does not add a second icon.
- The menu lists each registered HTTP/HTTPS handler once, with its icon and display name, in
  case-insensitive current-culture order; hidden registrations are absent.
- When Windows reports a default, exactly one item is checked and remains enabled. If no default
  can be read, no item is checked and the error is actionable.
- With HTTP and HTTPS assigned to different browsers, the HTTPS browser is checked, one
  non-actionable note names the HTTP browser, and no second checkmark appears.
- Selecting a different browser shows a native dialog naming it. Cancel leaves the state and
  checkmark unchanged.
- **Open Settings** launches the selected browser's page on supported Windows 11 builds when a
  registration name is available, and the Default apps page otherwise, without a UAC prompt.
- Before the user confirms in Settings, the app still shows the old default and never claims
  success. After the user confirms and reopens the menu, the checkmark and tooltip match Windows.
- Changes made outside the app are reflected on the next menu open.
- A Settings-launch failure produces a native error with the manual path and leaves the menu
  consistent.
- Quit works from the menu and by keyboard navigation, and the icon disappears.
- Read-only CLI probes run without a tray icon or mutex, report `none` instead of failing when no
  default is readable, and reject `--set-default` with a non-zero exit code.
- No app data files, preferences, startup entries, network access, or telemetry are created. The
  self-contained single-file host may extract bundled .NET runtime libraries to `%TEMP%\.net` on
  first launch; that is runtime behavior, not Defaulty data.
- The app runs as a standard user. It never writes to the registry, opens `UserChoice` directly,
  or automates the Settings UI.

## Safety and privacy

- Never edit Launch Services plists, the Windows registry, or `UserChoice` hashes directly.
- On Windows, Defaulty is read-only with respect to the registry. It reads
  `RegisteredApplications`, `Capabilities`, and `Classes\<ProgId>` keys plus the
  `CurrentVersion\UBR` version value used for the Settings build gate. It never opens
  `UserChoice` or its hash directly.
- Never change the host machine's default browser during automated tests.
- Manual verification that changes the default must restore the prior default afterward,
  unless the user explicitly asks to keep the new one.
- Never automate or bypass OS consent dialogs, including Windows Settings.
- No network, telemetry, analytics, or user data collection.
- No login item or background helper without a separate product decision.

## Roadmap

- **v1.0** — macOS menu bar app (shipped).
- **v1.0 (Windows)** - tray app implemented; manual validation and CI hardening remain.
- **Later** — evaluate launch-at-login and localization only if users ask.
