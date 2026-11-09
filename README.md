# Defaulty

Defaulty is a tiny menu bar app for switching the system default web browser on macOS.
Click the globe in the menu bar, pick a browser, and Defaulty sets it as the default for
both `http` and `https` links. No Dock icon, no windows, no background daemons.

## Features

- Menu bar–only agent app (`LSUIElement`), native AppKit.
- Lists installed browsers, sorted like Finder.
- The current default browser is checked.
- The menu re-reads installed browsers and the default every time it opens, so changes
  made elsewhere (such as Safari's "Make Default" prompt) are reflected immediately.
- Uses the supported `NSWorkspace` default-application API, not Launch Services plist
  editing.
- Keyboard quit (⌘Q) and accessible status/menu labels.
- Command-line modes for testing.

## Requirements

- macOS 13 or later
- Xcode / Swift 6 toolchain (developed with Swift 6.4 on macOS 27)

## Build and run

```sh
make build      # debug build
make test       # unit tests
make app        # build and ad-hoc sign build/Defaulty.app
make run        # build the app and open it
```

To install into `/Applications`:

```sh
make install
# or: make install INSTALL_DIR="$HOME/Applications"
```

You can also drag `build/Defaulty.app` to `/Applications` manually.

## Usage

1. Launch Defaulty. A globe appears in the menu bar.
2. Click it and choose a browser. The current default has a checkmark.
3. macOS may show a confirmation prompt asking whether to change your default web
   browser. Accept it; macOS requires this consent for the change to take effect.
4. Quit with **Quit Defaulty** in the menu (or press ⌘Q while the menu is open).

The status item's tooltip and accessibility label show the current default browser.

## CLI usage (for testers)

Run the executable inside the built app:

```sh
build/Defaulty.app/Contents/MacOS/Defaulty --list
build/Defaulty.app/Contents/MacOS/Defaulty --current
build/Defaulty.app/Contents/MacOS/Defaulty --set-default com.google.Chrome
build/Defaulty.app/Contents/MacOS/Defaulty --help
```

`--list` prints one browser per line as `bundleID<TAB>name<TAB>path`; the current default
is prefixed with `* `. `--current` prints the current default bundle identifier, or
`none`. `--set-default` makes the named browser the default for both schemes and exits
with a non-zero status on failure. The CLI modes never create the menu bar item.

## Uninstall

Quit Defaulty, then remove the app:

```sh
rm -rf /Applications/Defaulty.app
```

Defaulty stores no preferences or other files.

## Notes

- The app is ad-hoc signed for personal use and is **not notarized**. If Gatekeeper
  blocks a downloaded copy, right-click the app and choose **Open**, or remove the
  quarantine attribute: `xattr -dr com.apple.quarantine /Applications/Defaulty.app`.
- The app icon is generated reproducibly by `Scripts/make-icon.swift`:
  `swift Scripts/make-icon.swift` writes `Resources/AppIcon.icns`.
