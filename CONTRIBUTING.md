# Contributing

## Workflow

1. Branch from `main`: `feat/<topic>`, `fix/<topic>` or `chore/<topic>`.
2. Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/): `feat: add weekly summary`, `fix: streak breaks on DST change`.
3. Add a line under `## [Unreleased]` in `CHANGELOG.md` (CI checks this when `src/` changes; label the PR `skip-changelog` to bypass).
4. Run `./scripts/validate.ps1` before opening the PR. CI runs the same script.

## Where tests go

| Change | Test |
|---|---|
| Logic (schedule, goals, streaks, heatmap, persistence) | `tests/ActivityTracker.Core.Tests`: put logic in `Core` so it can be unit tested |
| A component or page behavior | `tests/ActivityTracker.Web.Tests` (bUnit) |
| A complete user journey across pages, storage, routing | `tests/ActivityTracker.E2E` (Playwright) |

Tests use a fixed clock (`FakeTimeProvider`, Monday 16 Nov 2026) so dates are deterministic. Never use `DateTime.Now` in app code: go through `TrackerService.Now` / `Today`.

## Data format changes

The persisted JSON has a `schemaVersion` (`TrackerData.CurrentSchemaVersion`).
Adding optional fields needs no change. A breaking change bumps the schema version, adds a migration in `TrackerJson.Deserialize`, and is a **major** version bump of the app.
