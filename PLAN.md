# Defaulty — implementation plan

A tiny macOS menu bar app for switching the system default web browser.

## Product requirements

- Menu bar–only app (no Dock icon, no main window).
- Status bar item shows a template SF Symbol (globe) that adapts to light/dark menu bars.
- Clicking the item opens an `NSMenu` listing installed browsers.
- The current default browser is marked with a checkmark.
- Choosing another browser sets it as the system default for both `http` and `https`.
- The list refreshes every time the menu opens, so external changes are reflected.
- Clean, native AppKit behavior: keyboard quit (⌘Q), accessible labels, no custom chrome.
- A clear error alert if the system rejects a change.

## Architecture

- Swift Package Manager project, native AppKit (no SwiftUI/MenuBarExtra needed).
- `DefaultyCore` library:
  - `Browser` value type.
  - `BrowserDiscovering` protocol + `LaunchServicesBrowserDiscovery` (union of
    `LSCopyAllHandlersForURLScheme("http"/"https")`, resolve app bundle, dedupe).
  - `BrowserCatalog` (filter self, sort by localized name).
  - `DefaultBrowserService` (reads current default via
    `NSWorkspace.urlForApplication(toOpen:)`, sets `https` then `http` via
    `NSWorkspace.setDefaultApplication(at:toOpenURLsWithScheme:completion:)`).
- `Defaulty` executable:
  - `main.swift` — CLI modes (`--list`, `--current`, `--set-default <bundle-id>`) for
    testing, otherwise starts the agent app.
  - `AppDelegate` + `StatusMenuController` — status item, menu, actions.
- `Resources/Info.plist` — `LSUIElement=true`, bundle metadata, min macOS 13.
- `Scripts/build-app.sh` — builds, assembles `build/Defaulty.app`, ad-hoc codesigns.
- `Makefile` — `build`, `test`, `app`, `run`, `install`, `clean`.
- Unit tests for catalog dedupe/filter/sort using a fake discovery; integration test
  for Launch Services discovery. No test changes the system default.
- CLI round-trip test is run manually during validation (set Chrome, verify, restore Safari).

## macOS guidelines

- Agent app (`LSUIElement`) with a single status item.
- Template SF Symbol icon; tooltip/accessibility label reflects current default.
- Native menu with checked state, separators, and a standard Quit item.
- Use supported system APIs; never edit LaunchServices plists directly.
- Menu rebuilds on open (`menuNeedsUpdate`) so state is always truthful.
- No sandbox (local developer tool); ad-hoc signature is sufficient for local use.

## Validation

1. `swift build` and `swift test`.
2. Build the `.app`, verify `Info.plist`, `LSUIElement`, ad-hoc signature.
3. Launch the app; user visually confirms icon, menu, checkmark, and switching.
4. CLI: list browsers, set a browser, confirm `current`, restore original.
5. Confirm no Dock icon and that the app quits cleanly via ⌘Q.
