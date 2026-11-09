# Defaulty for macOS

The macOS implementation of Defaulty: a tiny menu bar app for switching the system default
web browser. Built with Swift 6 and AppKit, packaged as an `LSUIElement` agent app with no
Dock icon.

See the shared [product spec](../../docs/product-spec.md) and
[architecture](../../docs/architecture.md) for behavior and design. Implementation rules for
agents are in [AGENTS.md](AGENTS.md).

## Features

- Globe icon in the menu bar; no windows, Dock icon, or background helpers.
- Native menu listing installed web handlers, sorted like Finder, with app icons.
- Checkmark on the current default browser; the menu re-reads state every time it opens.
- Choosing a browser sets it as the default for both `http` and `https` using supported
  `NSWorkspace` APIs.
- Native error alert with a recovery hint if macOS refuses the change.
- ⌘Q and a standard Quit menu item.
- No preferences, network access, or stored data.

## Build

Requirements: macOS 13 or later, Xcode/Swift 6 toolchain.

From the repository root:

```sh
make macos-build    # debug build
make macos-test     # unit tests
make macos-verify   # tests + app bundle + Info.plist + signature
make macos-app      # build apps/macos/build/Defaulty.app and ad-hoc sign it
make macos-run      # build and open the app
make macos-install  # install to /Applications
```

Or from this directory:

```sh
swift build
swift build -c release
swift test
make verify
make run
make install
make install INSTALL_DIR="$HOME/Applications"
```

The packaged app is written to `apps/macos/build/Defaulty.app` (relative to this directory:
`build/Defaulty.app`). You can also drag it to `/Applications` manually.

## Usage

1. Launch Defaulty. A globe appears in the menu bar.
2. Click it and choose a browser. The current default has a checkmark.
3. macOS may ask you to confirm the change; accept it if prompted.
4. Quit with **Quit Defaulty** or ⌘Q while the menu is open.

The status item's tooltip and accessibility label name the current default browser.

## CLI probes

Run the executable inside the built app so bundle metadata is correct:

```sh
BIN=build/Defaulty.app/Contents/MacOS/Defaulty
"$BIN" --list
"$BIN" --current
"$BIN" --set-default com.google.Chrome
"$BIN" --help
```

`--list` prints one browser per line as `bundleID<TAB>name<TAB>path`; the current default is
prefixed with `* `. `--current` prints the default bundle identifier or `none`.
`--set-default` changes the host machine's default for both schemes — use it deliberately and
restore the previous default afterward. The CLI modes never create the menu bar item.

## Tests

```sh
swift test
```

Tests cover catalog exclusion, deduplication, and Finder-style sorting, plus a read-only
Launch Services discovery integration test. No test changes the system default browser.

## Packaging

- `Scripts/build-app.sh` builds release, assembles the bundle, copies `Info.plist`, `PkgInfo`,
  and the optional icon, then ad-hoc signs it and fails if verification fails.
- `Resources/Info.plist` sets `LSUIElement=true`, the bundle identifier
  `com.miguels.defaulty`, minimum macOS 13, and the utilities category.
- The icon is generated reproducibly by `Scripts/make-icon.swift`:
  `swift Scripts/make-icon.swift` writes `Resources/AppIcon.icns`.
- `make install` creates the destination directory if needed.

## Notes and limitations

- The app is ad-hoc signed for personal use and is **not notarized**. If Gatekeeper blocks a
  downloaded copy, right-click it and choose **Open**, or remove the quarantine attribute:
  `xattr -dr com.apple.quarantine /Applications/Defaulty.app`.
- Any app macOS knows can open `http`/`https` links can appear in the list — this is the same
  set the system considers for "Default web browser". For example, ChatGPT registers as a web
  handler on some systems.
- Builds are native-architecture (arm64 on Apple Silicon). Add universal-build support if you
  need to distribute to Intel Macs.
- Uninstall: quit the app and delete `/Applications/Defaulty.app`. Nothing else is stored.

## Visual validation

Agents cannot verify the menu bar visually. Use the macOS checklist in
[docs/validation.md](../../docs/validation.md) and ask the user to confirm icon rendering,
menu layout, checkmark placement, switching, and quit behavior.
