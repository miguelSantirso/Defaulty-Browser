# AGENTS.md — Windows app (planned)

This directory has **no implementation yet**. Read the root `AGENTS.md`,
`docs/product-spec.md`, and `docs/architecture.md` before making changes.

## Rules

- Do not commit an implementation before the Windows research spike updates
  `docs/product-spec.md` and `docs/architecture.md`.
- Windows default-app associations are user-controlled. Never modify the registry or
  `UserChoice` hashes, and never automate or bypass the Settings consent flow.
- The shared UX contract adapts on Windows: Defaulty guides the user through the official
  Settings flow, then re-reads the current default.
- Keep all Windows code, tests, packaging, and docs in this directory.
- Add namespaced root Makefile targets (`windows-*`), CI, and validation steps with the first
  implementation.
- Prefer a current .NET LTS and native tray/menu behavior. Add dependencies only with a
  documented reason.

## Expected structure (to confirm during the research spike)

```text
apps/windows/
├── AGENTS.md
├── README.md
├── src/…              # tray app
├── tests/…            # non-destructive tests
└── packaging/…        # build/sign scripts
```

Do not create empty placeholder projects. Land the research/design update first.
