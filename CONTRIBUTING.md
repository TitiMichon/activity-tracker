# Contributing

## Workflow

1. Every change starts from a GitHub issue (create one if needed).
2. Branch from `main`, one branch per issue: `<issue-number>_<name-of-feature>`, e.g. `6_reminder-time-reset`.
3. Split commits by concern (code, tests, docs). Message format: `<type>: <message> (#<issue-number>)`,
   with type one of `feat`, `fix`, `refacto`, `test`, `docs`, `ci`, `chore`, `perf`, `style`,
   e.g. `fix: keep the typed reminder time (#6)`.
4. Add a line under `## [Unreleased]` in `CHANGELOG.md` (CI checks this when `src/` changes; label the PR `skip-changelog` to bypass).
5. Run `./scripts/validate.ps1`, then open a pull request with `Closes #<issue-number>`. CI runs the same script.

## Where tests go

| Change | Test |
|---|---|
| Logic (schedule, goals, streaks, heatmap, persistence) | `tests/ActivityTracker.Core.Tests`: put logic in `Core` so it can be unit tested |
| A component or page behavior | `tests/ActivityTracker.Web.Tests` (bUnit) |
| A complete user journey across pages, storage, routing | `tests/ActivityTracker.E2E` (Playwright) |

Tests use a fixed clock (`FakeTimeProvider`, Monday 16 Nov 2026) so dates are deterministic. Never use `DateTime.Now` in app code: go through `TrackerService.Now` / `Today`.

## Data format changes

The persisted JSON has a `schemaVersion` (`TrackerData.CurrentSchemaVersion`).
Adding optional fields needs no change. Any other change bumps the schema version and adds a step to `TrackerMigrations` (it works on the raw JSON, so it can rename or reshape fields), with a test that a backup from the previous version still imports.
Data is always upgraded automatically, so this is a **minor** version of the app (see [ROADMAP.md](ROADMAP.md#versioning)). Older apps refuse newer data with "Please update the app".

| Schema | App | Change |
|---|---|---|
| 1 | 1.0 | Activities, entries, settings |
| 2 | 1.2 | Tasks |
