# Validation

Validation is split into automated checks (safe, read-only) and manual UI checks (which may
change the system default). Always prefer the automated checks first.

## Safety rules

- Automated tests must **never** change the host machine's default browser.
- The macOS integration test only reads Launch Services.
- The `--set-default` CLI mode changes system state. Use it only for deliberate validation,
  and restore the previous default afterward unless the user asks otherwise.
- Record the prior default before any manual switch:

```sh
/Applications/Defaulty.app/Contents/MacOS/Defaulty --current
```

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

## Windows (planned)

Add the equivalent automated build/test commands, a read-only discovery check, and a manual
tray checklist when the Windows implementation exists. Explicitly verify that the app never
attempts to bypass Windows' default-app protections.
