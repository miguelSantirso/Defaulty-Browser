# AGENTS.md

Instructions for coding agents working in this repository. Read this first, then the
`AGENTS.md` inside the platform directory you are changing.

## What this project is

Defaulty is a tiny cross-platform utility for switching the operating system's default web
browser from a menu bar / notification-area icon. The macOS app is shipped. The Windows app is
designed; implementation is planned. There is no shared runtime code yet.

## Repository map

| Path | Contents |
| --- | --- |
| `apps/macos/` | Swift 6 + AppKit menu bar app, packaging, tests, CLI probes |
| `apps/windows/` | C# / .NET 10 tray app design (no implementation yet) |
| `docs/product-spec.md` | Source of truth for behavior and acceptance criteria |
| `docs/architecture.md` | Repo boundaries, platform mappings, constraints, decisions |
| `docs/validation.md` | Automated and manual validation procedures |
| `AGENTS.md` | These repo-wide rules |
| `Makefile` | Root orchestration (`make macos-*`) |

## Source-of-truth order

1. `docs/product-spec.md` — behavior. Update it before changing behavior.
2. `docs/architecture.md` — structure and technical constraints.
3. Platform `AGENTS.md` — implementation rules and commands.
4. Code and tests.

If code and docs disagree, treat it as a bug and fix the docs or the code in the same change.

## Commands

Run from the repository root unless noted.

```sh
make help
make check          # automated checks (currently macOS tests)
make macos-build
make macos-test
make macos-verify   # tests + app bundle + plist + signature
make macos-run
make macos-install                  # /Applications
make macos-install INSTALL_DIR=...  # custom destination
make macos-clean
```

Platform-local equivalents live in `apps/macos/Makefile` and `apps/macos/README.md`.

Windows commands are intentionally absent until an implementation exists.

## Golden rules

- **Keep platforms isolated.** Put platform code under `apps/<platform>/`. Do not put OS-specific
  code, build outputs, or dependencies in the repository root.
- **Do not invent a shared framework.** Share behavior through `docs/product-spec.md`. Extract
  shared code only when a real second implementation needs it, and document the boundary.
- **Never silently change the host's default browser in automated tests.** Tests and CI must be
  read-only with respect to system defaults. Manual validation that changes the default must
  restore it unless the user explicitly asks to keep the new value.
- **Use supported OS APIs.** Never edit Launch Services plists, the Windows registry, or
  `UserChoice` hashes. Never automate or bypass OS consent dialogs.
- **Respect the agent-app model.** macOS: `LSUIElement`, no Dock icon, one status item.
  Windows: tray app, no main window.
- **Keep the app dependency-free by default.** Adding a dependency requires a concrete reason
  and a note in `docs/architecture.md`.
- **Do not commit build outputs.** `.build/`, `build/`, `bin/`, `obj/`, archives, and signing
  material stay untracked.
- **Signing:** local macOS builds are ad-hoc signed. Do not add certificates, profiles, or
  notarization credentials to the repo.
- **Accessibility and localization:** use native controls and provide accessibility labels.
  English strings are fine until localization is explicitly requested.

## Implementation workflow

1. Read `docs/product-spec.md`, `docs/architecture.md`, and the platform `AGENTS.md`.
2. Plan the smallest change that satisfies the shared UX contract.
3. Implement inside the correct `apps/<platform>/` directory; add or update tests.
4. Run the platform build, tests, and packaging checks from `docs/validation.md`.
5. Update docs when behavior, commands, layout, or constraints change.
6. Report exact commands and results. Do not claim visual verification you could not perform.
7. Leave the working tree clean except for the intended change.

## Definition of done

- [ ] Build succeeds with no new warnings (or warnings are explained).
- [ ] Automated tests pass.
- [ ] Packaged app/bundle is produced when the change affects packaging.
- [ ] System-default safety rules respected.
- [ ] `docs/product-spec.md` / `docs/architecture.md` / platform docs updated if needed.
- [ ] Manual UI items that need human eyes are listed for the user.
- [ ] `git status` contains only intended changes.

## Commit conventions

Use Conventional Commits:

```text
feat(macos): add launch-at-login toggle
fix(macos): keep checkmark in sync after a failed switch
docs: clarify Windows default-app constraints
refactor(repo): move platform apps under apps/
chore: update CI actions
```

Keep commits focused. Do not amend or rewrite published history unless asked.

## Validation and visual checks

- Automated checks must stay read-only with respect to the system default browser.
- Visual checks (menu bar icon, menu layout, checkmark, alert appearance, light/dark rendering)
  require a human. Provide the user with a short, numbered checklist from `docs/validation.md`.
- When a manual test changes the default browser, verify the restoration step explicitly.

## Adding or changing a platform

Follow the checklist at the end of `docs/architecture.md`. At minimum: update the product spec,
create `apps/<platform>/` with its own `AGENTS.md`, wire namespaced commands into the root
`Makefile`, add CI, and add validation steps.
