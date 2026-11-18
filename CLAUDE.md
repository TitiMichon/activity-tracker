# Activity Tracker – working conventions

Blazor WebAssembly PWA (.NET 10). See README.md for layout and CONTRIBUTING.md for the full workflow.

## Git workflow (mandatory)

- Every change starts from a GitHub issue in `TitiMichon/activity-tracker`. If none exists, create one first (`gh issue create`).
- One branch per issue, created from an up-to-date `main`:
  `<issue-number>_<name-of-feature>` — e.g. `6_reminder-time-reset`.
- Never commit directly to `main`; open a pull request that closes the issue (`Closes #6` in the body).
- Split commits by concern (e.g. the fix, its tests, docs/CHANGELOG), each building on its own when possible.
- Commit message format: `<type>: <message> (#<issue-number>)`
  - types: `feat`, `fix`, `refacto`, `test`, `docs`, `ci`, `chore`, `perf`, `style`
  - e.g. `fix: keep the typed reminder time (#6)`

## Validation

- `./scripts/validate.ps1` must pass before pushing (build, unit, component, publish, E2E).
- Add an entry under `## [Unreleased]` in CHANGELOG.md for user-visible changes (CI checks it on PRs).

## Tools

- GitHub CLI: `C:\Program Files\GitHub CLI\gh.exe` (may not be on PATH in older shells).
