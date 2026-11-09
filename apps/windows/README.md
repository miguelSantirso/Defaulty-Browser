# Defaulty for Windows (planned)

This directory is reserved for the Windows tray implementation. Nothing is implemented yet.

## Why it is not a direct port

Windows 10/11 protect default-app associations. The default browser is stored in a per-user
`UserChoice` entry that includes a hash, and Microsoft does not provide a supported API for a
third-party app to silently set the default browser. Defaulty must not bypass that protection.

The Windows UX will therefore be **user-mediated**: Defaulty lists browsers, marks the current
default, and guides the user through the official Windows **Settings → Default apps** flow,
then re-reads the result.

Read `docs/product-spec.md` and `docs/architecture.md` before starting. The research spike must
confirm:

- how to enumerate installed browsers reliably;
- which supported Settings deep link is appropriate;
- how to read the current default without touching protected internals;
- the exact UX and acceptance criteria for a user-mediated change.

## Intended stack

- C# on a current .NET LTS.
- Native notification-area icon and context menu; no main window.
- No registry patching, no `UserChoice` hash manipulation, no automation of consent dialogs.
- Self-contained packaging and signing approach to be decided.

## Commands

None yet. When the project is added, expose namespaced targets from the repo root, for example:

```sh
make windows-build
make windows-test
make windows-run
```

Keep all Windows-specific code in this directory. Update `docs/product-spec.md`,
`docs/architecture.md`, `docs/validation.md`, the root `Makefile`, and CI in the same change.
