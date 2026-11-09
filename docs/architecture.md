# Architecture

Defaulty is a small cross-platform utility with **one shared product contract** and
**independent native implementations per platform**. Keep platform code inside its app
directory; share behavior through documentation, not through a runtime framework.

## Repository boundaries

| Path | Purpose |
| --- | --- |
| `apps/macos/` | Swift 6 + AppKit implementation, packaging, and tests |
| `apps/windows/` | Reserved for the planned C# / .NET tray implementation |
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

## Windows implementation (planned)

Do not implement until the research spike updates `docs/product-spec.md`.

Likely shape:

| Responsibility | Candidate approach |
| --- | --- |
| Runtime | C# on a current .NET LTS; no cross-platform UI framework required |
| Presentation | Notification-area icon + native context menu |
| Discovery | Registered browser capabilities and/or Start Menu app enumeration (TBD) |
| Change | User-mediated: open the relevant Settings → Default apps page; never modify `UserChoice` |
| Packaging | Self-contained or framework-dependent build, signed for distribution |

Hard constraints:

- Windows protects default-app associations with a per-user `UserChoice` hash.
- There is no supported, silent API for third-party apps to set the default browser.
- Do not use undocumented hash bypasses, registry patching, or automation of consent dialogs.
- The shared UX contract adapts: Defaulty guides the user through Settings, then re-reads state.

## Adding a platform

1. Update `docs/product-spec.md` with platform behavior and acceptance criteria.
2. Add `apps/<platform>/` with its own `AGENTS.md` and `README.md`.
3. Add isolated build/test/package commands and wire them into the root `Makefile`.
4. Add CI for that platform.
5. Add validation steps to `docs/validation.md`.
6. Verify the shared acceptance criteria and document any OS-mandated differences.
