# Defaulty — product specification

Status: **macOS v1.0.0 implemented. Windows planned.**
This document is the source of truth for behavior. Change it before changing behavior.

## Vision

Defaulty is a tiny utility that lets you switch the operating system's default web
browser from the menu bar (macOS) or notification area (Windows). One icon, one short
menu, no settings window, no account, no telemetry.

## Goals

- Make changing the default browser a two-click operation.
- Always show the truth: the menu reflects the current system default every time it opens.
- Use only supported, user-respecting platform APIs.
- Stay small: a single agent app with no windows, background daemons, or preferences.
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
   confirmation, the app explains what to do instead of bypassing the OS.
6. **Fresh state** — the menu re-reads installed browsers and the current default every
   time it opens; changes made elsewhere are reflected.
7. **Errors** — failures are surfaced in a native alert with a recovery hint.
8. **Quit** — an obvious quit command with the platform-standard keyboard shortcut.
9. **Privacy** — no network access, analytics, preferences, or stored history.
10. **Tiny footprint** — launch quickly, use negligible memory, and leave no background work.

### Acceptance criteria

- Launching the app shows exactly one status/tray icon and no main window.
- Opening the menu shows every installed web handler once, with app icons and names.
- The current default has a checkmark; exactly one item is checked.
- Choosing a different browser updates the system default and the checkmark.
- Changing the default outside the app is reflected the next time the menu opens.
- A failed change produces a native, actionable error and leaves the menu consistent.
- Quit works from the menu and the standard shortcut.
- No files, preferences, or login items are created.

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

### Windows (planned)

Windows default-app associations are user-controlled: Windows 10/11 protect browser
associations with a per-user `UserChoice` hash, and Microsoft does not provide a supported
silent API for a third-party app to set the default browser. Defaulty must not bypass this.

The Windows experience will therefore be user-mediated. The likely flow is:

1. Defaulty lists discovered browsers and marks the current default.
2. Choosing a browser opens the relevant Windows **Settings → Default apps** page (or
   otherwise guides the user there) with clear, minimal instructions.
3. The user confirms the change in Windows Settings.
4. Defaulty re-reads the association and shows the resulting checkmark.

Discovery mechanism, supported deep links, and the exact UX must be confirmed in a
Windows research spike before implementation. They must be documented here first.

## Safety and privacy

- Never edit Launch Services plists, the Windows registry, or `UserChoice` hashes directly.
- Never change the host machine's default browser during automated tests.
- Manual verification that changes the default must restore the prior default afterward,
  unless the user explicitly asks to keep the new one.
- No network, telemetry, analytics, or user data collection.
- No login item or background helper without a separate product decision.

## Roadmap

- **v1.0** — macOS menu bar app (shipped).
- **Next** — Windows tray app research spike and implementation.
- **Later** — evaluate launch-at-login and localization only if users ask.
