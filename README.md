# Defaulty

One tiny icon to switch your default web browser.

Defaulty lives in the menu bar (macOS) or notification area (Windows), lists the browsers
installed on your system, and marks the current default with a checkmark. Pick another
browser and it becomes the system default for web links. No windows, no account, no
telemetry.

**Status:** macOS v1.0.0 is implemented. Windows tray app (Phase 3) is implemented with CI/packaging hardening in progress.

## Repository layout

```text
.
├── apps/
│   ├── macos/          Swift 6 + AppKit menu bar app, tests, packaging
│   └── windows/        C#/.NET 10 tray app (implemented: CLI, packaging, tests)
├── docs/
│   ├── product-spec.md    Shared behavior and acceptance criteria
│   ├── architecture.md    Repo boundaries, platform mappings, decisions
│   └── validation.md      Automated and manual validation
├── AGENTS.md           Instructions for coding agents
└── Makefile            Root orchestration
```

Platform code stays inside `apps/<platform>/`; product behavior is shared through the spec.

## Quick start (macOS)

Requirements: macOS 13+, Xcode/Swift 6 toolchain.

```sh
make macos-run       # build, ad-hoc sign, and launch
make macos-install   # install to /Applications
make check           # automated checks
```

Install somewhere else:

```sh
make macos-install INSTALL_DIR="$HOME/Applications"
```

Full macOS instructions, CLI probes, and packaging notes live in
[`apps/macos/README.md`](apps/macos/README.md).

## Documentation

- [Product specification](docs/product-spec.md) — behavior, goals, platform differences,
  Windows constraints.
- [Architecture](docs/architecture.md) — repository boundaries, macOS mapping, Windows plan.
- [Validation](docs/validation.md) — how to verify a change, including the manual UI checklist.
- [Agent instructions](AGENTS.md) — rules and commands for coding agents.

## Windows

The Windows tray app is implemented and uses the official Windows
Settings flow because Windows has no supported silent default-browser API. See
[`apps/windows/README.md`](apps/windows/README.md). Defaulty will not bypass Windows'
default-app protections.

## Notes

- macOS builds are ad-hoc signed and not notarized; they are intended for personal use.
- The app stores nothing on disk: no preferences, history, or login items.
- Any app macOS knows can open `http`/`https` links can appear in the browser list, matching
  what the system itself considers a web handler.
