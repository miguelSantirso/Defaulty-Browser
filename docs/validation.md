# Validation

Validation is split into automated checks (safe, read-only) and manual UI checks (which may
change the system default). Always prefer the automated checks first.

## Safety rules

- Automated tests must **never** change the host machine's default browser on any platform.
- The macOS integration test only reads Launch Services.
- Windows automated tests only read registry registration data, association strings, and the
  `UBR` version value. They never launch Settings, never access `UserChoice` directly, and never
  write to the registry.
- The macOS `--set-default` CLI mode changes system state. Use it only for deliberate validation,
  and restore the previous default afterward unless the user asks otherwise.
- Windows has no `--set-default` mode by design. `--open-settings` is a user-visible action and is
  therefore manual-only, never part of an automated test.
- Record the prior default before any manual switch:

```text
# macOS
/Applications/Defaulty.app/Contents/MacOS/Defaulty --current

# Windows (once the app exists)
apps\windows\artifacts\win-x64\Defaulty.exe --current
```

- Restore the prior default in the OS UI after manual Windows validation, then re-run `--current`.

## Automated — macOS

From the repository root:

```sh
make check          # currently runs macOS unit tests
make macos-build    # debug build
make macos-test     # unit tests
make macos-verify   # tests + app bundle + Info.plist + signature checks
```

Or from `apps/macos/`:

```sh
swift build
swift build -c release
swift test
make verify
```

`make macos-verify` must confirm:

- `swift test` passes;
- `apps/macos/build/Defaulty.app` is assembled and ad-hoc signed;
- `Info.plist` passes `plutil -lint` and has `LSUIElement = true`;
- `codesign --verify --strict` succeeds.

## CLI checks — macOS (read-only)

Run the executable inside the built app so bundle metadata is available:

```sh
BIN=apps/macos/build/Defaulty.app/Contents/MacOS/Defaulty
"$BIN" --help
"$BIN" --list       # current default prefixed with "* "
"$BIN" --current
"$BIN" --set-default com.example.does-not-exist   # must exit 1 without changing anything
```

## Manual UI checklist — macOS

Use a user who can see the screen; agents cannot verify visuals.

- [ ] Launching shows one globe icon in the menu bar and no Dock icon.
- [ ] The icon renders correctly in light and dark menu bars (template behavior).
- [ ] Hovering shows a tooltip naming the current default browser.
- [ ] Clicking opens a native menu listing installed web handlers once, with icons.
- [ ] Exactly the current default is checked; the checked item is enabled.
- [ ] Choosing another browser changes the system default and the checkmark.
- [ ] Choosing the already-checked browser is harmless.
- [ ] Opening the menu after an external change shows the updated default.
- [ ] ⌘Q and the Quit menu item terminate the app.
- [ ] The menu bar item disappears after quitting.
- [ ] An invalid/removed app does not leave a stale, clickable item.

## Manual checks — bundle

```sh
plutil -lint apps/macos/build/Defaulty.app/Contents/Info.plist
codesign --verify --strict apps/macos/build/Defaulty.app
codesign -dv apps/macos/build/Defaulty.app
lsappinfo info -app Defaulty | grep -o 'type="[^"]*"'   # type="UIElement"
```

## Automated — Windows (planned; lands with the first implementation)

The first Windows implementation must add these namespaced root targets and make them pass.
Until they exist, run the equivalent commands from `apps/windows/`.

```sh
make windows-build
make windows-test
make windows-verify
```

Or from `apps/windows/`:

```powershell
dotnet build Defaulty.sln -c Release
dotnet test Defaulty.sln -c Release
```

`make windows-verify` must confirm:

- the solution builds with no new warnings;
- unit tests pass;
- the read-only integration tests pass;
- the static safety audit passes (no registry writes, association setters, direct `UserChoice`
  access, Settings automation, network, startup registration, or elevation);
- `packaging/app.manifest` requests `asInvoker` and PerMonitorV2;
- a self-contained single-file `win-x64` artifact is produced.

### CLI checks — Windows (read-only)

Run from the publish directory:

```powershell
Defaulty.exe --help
Defaulty.exe --list       # current https default prefixed with "* "
Defaulty.exe --current    # https=... / http=...
Defaulty.exe --diagnose   # OS build/UBR, registrations, current state, Settings-link mode
Defaulty.exe --set-default anything   # must exit non-zero and change nothing
```

Manual-only, never in tests:

```powershell
Defaulty.exe --open-settings   # opens the Default apps page; user-visible action
```

Read-only modes run headless: no tray icon and no single-instance mutex, so they work while the
tray app is running. They exit 0 even when no default can be resolved.

Expected shapes:

- `--list`: one browser per line as `id<TAB>name<TAB>progId<TAB>exe`; the canonical `https`
  default is prefixed with `* `. The command never opens Settings and prints nothing when no
  browser is registered.
- `--current`: prints `https=<progId><TAB><name>` and `http=<progId><TAB><name>`, or `none`
  when a scheme cannot be resolved.
- `--diagnose`: machine-readable diagnostics only; no network and no user data.
- `--help`: prints usage and exits 0; unknown arguments exit non-zero.
- `--set-default`: not a supported mode on Windows. It must be rejected with a non-zero exit
  code and must not touch the registry. This is deliberate: Windows has no supported silent
  setter.

Tests must not invoke `--open-settings`, because that launches Settings. They must not change
the host default. The first implementation must add a `windows-latest` CI job.

Manual check: while the tray app is running, run `--list` and `--current`; both must complete
without adding a second tray icon or blocking on the single-instance mutex.

## Manual UI checklist — Windows

Use a user who can see the screen; agents cannot verify visuals. Test on Windows 11 (23H2 or
later; include 24H2/25H2 if available). Test Windows 10 22H2 best-effort. Before starting,
record the current default with `--current`; after testing, restore it in Settings and verify
with `--current`.

### Tray, menu, and state

- [ ] Launching shows exactly one globe icon in the notification area, no taskbar button, and no
      main window.
- [ ] Launching a second copy does not add a second icon.
- [ ] Hovering the icon shows a tooltip naming the current default browser.
- [ ] Left-click and right-click both open the same menu.
- [ ] The menu lists each installed/registered browser once, with its icon and display name,
      sorted case-insensitively.
- [ ] Exactly one browser is checked when Windows reports a default; the checked item is enabled.
- [ ] Choosing the checked browser closes the menu without changing anything.
- [ ] Keyboard-only use works: Shift+F10 or the Menu key, arrow keys, Enter, Esc.
- [ ] Quit works from the menu and by keyboard navigation; the icon disappears.

### User-mediated change

- [ ] Choosing a different browser shows a native dialog naming that browser, with
      **Open Settings** and **Cancel**. The app does not claim the change succeeded.
- [ ] **Cancel** leaves the default and checkmark unchanged.
- [ ] **Open Settings** opens the selected browser's page on a supported Windows 11 build, or the
      Default apps page on Windows 10/older builds.
- [ ] No UAC/elevation prompt appears.
- [ ] Do **not** change anything in Settings; reopen the menu. The old default is still checked.
- [ ] In Settings, use **Set default** (Windows 11) or the **Web browser** dropdown (Windows 10)
      to choose the browser; return to Defaulty and reopen the menu. The checkmark and tooltip
      now show the new browser.
- [ ] Changing the default outside Defaulty (Settings or another app) is reflected the next time
      the menu opens.

### Edge cases

- [ ] HTTP and HTTPS assigned to different browsers: the HTTPS browser is checked, one
      non-actionable note names the HTTP browser, and no second checkmark appears. Fixing it in
      Settings clears the note.
- [ ] A removed/uninstalled browser does not leave a stale, clickable item.
- [ ] If the current default is not in the registered list, a synthetic checked item still shows
      the truth and **Open Default apps…** works.
- [ ] **Open Default apps…** always opens the Default apps page.
- [ ] A failed Settings launch (covered by the launcher unit test with a fake) produces an
      actionable native error naming **Settings → Apps → Default apps**; the menu stays consistent.

### Footprint, privacy, and accessibility

- [ ] No app files, preferences, history, or startup entries are created (`shell:startup`, Task
      Manager → Startup apps, `%APPDATA%`, `%LOCALAPPDATA%`). The single-file host may populate
      `%TEMP%\.net` with extracted runtime libraries; that is expected.
- [ ] The app makes no network connections (verify with Resource Monitor or Windows Firewall).
- [ ] The app runs as a standard user; Task Manager shows it is not elevated.
- [ ] The tray icon, menu, checkmarks, icons, and guidance dialog render correctly in light mode,
      dark mode, and high contrast.
- [ ] Layout is usable at 100%, 150%, and 200% display scaling.
- [ ] Narrator reads the tray tooltip and menu items; all actions are reachable by keyboard.

### Verify Windows protections are never bypassed

Run the static safety audit and confirm it passes. Then perform this manual observation:

- [ ] Start Process Monitor with filters for `Process Name is Defaulty.exe` and operations
      `RegSetValue`, `RegCreateKey`, `RegDeleteKey`, and `RegSetInfoKey`. Select a browser, open
      Settings, complete a change, and reopen the menu. Defaulty.exe performs **no** registry
      writes.
- [ ] Add a filter for `Path contains UserChoice`. Defaulty.exe must perform **no write** or
      `RegCreateKey`/`RegDeleteKey` operation on `UserChoice`. Reads performed inside
      `AssocQueryString` are allowed because that is the supported read API; Defaulty never opens
      the key directly. The final change is written only by Windows Settings after the user's
      explicit action.
- [ ] Confirm the process is not elevated and the manifest is `asInvoker`.
- [ ] Confirm there is no UI Automation or keystroke injection: Defaulty opens Settings but does
      not click, type, or drive the Settings UI. The user performs the change.
- [ ] Review the source for `IAssocHandler::MakeDefault`, `IApplicationAssociationRegistration`,
      `SetUserFTA`, `SetDefaultBrowser`, `UserChoice`, `SendKeys`, and UI Automation. None may be
      used to change an association.

If any of these checks fails, stop and fix the implementation; do not ship a workaround.

### Restore after manual testing

- [ ] Set the original default browser back in Settings.
- [ ] Run `Defaulty.exe --current` and confirm it matches the recorded value.
- [ ] Close Settings and Defaulty if they were left open.
