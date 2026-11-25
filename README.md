# Activity Tracker

Track anything you do (music, sport, reading, mood, habits) on a calendar, with objectives and heatmaps.
It is a Blazor WebAssembly progressive web app: it runs entirely in the browser, works offline, can be installed on a phone or desktop, and keeps your data on your device.

## Features (v1)

| | |
|---|---|
| **Activities** | Name, emoji logo, color. Three kinds: *Check* (done/not done), *Quantity* (value + unit, e.g. 30 min), *Scale* (options with their own icon/color/score, e.g. 😫 → 😄 mood). |
| **Periodicity** | Anytime, every day, chosen weekdays, every N days, monthly. Start date and optional deadline. Times per day (e.g. water 8×) with "3 / 8" progress. |
| **Reminders** | A time per planned activity: if not done by then, a notification (while the app is open, in a tab or installed). |
| **Objectives** | "3× per week", "150 min per week", "500 pages until the deadline"… with progress bars. |
| **Today** | The home screen to plan the day: a to-do list for today (unfinished items carry over), the activities scheduled today with their reminder time, and a long-term to-do list. Other activities, today's log and objectives are folded below. |
| **Quick add** | One tap logs a Check activity, Quantity/Scale open a mini picker. Undo in the toast. |
| **Custom look per entry** | Any entry can override its icon and color (e.g. a special 🎉 day). |
| **Calendar** | Day / week / month views, filter by activity, planned-but-missed markers. Activities can be hidden from the calendar. |
| **Heatmap** | Last 12 months or a given year, for all activities, one activity, or one per activity. Each day takes the color chosen that day (mood option, or the entry's custom color). Streaks and totals. |
| **Data** | Stored in the browser (localStorage). JSON export/import for backups and moving between devices. |

## Getting started

Requirements: [.NET SDK 10](https://dotnet.microsoft.com/download) (see `global.json`).

```powershell
dotnet run --project src/ActivityTracker.Web      # then open the URL printed in the console
```

### Android app

The Android app (`src/ActivityTracker.Mobile`, .NET MAUI Blazor Hybrid) shows the same screens as the web app,
stores its data in the app's private folder, exports backups through the share sheet and uses Android notifications.

Requirements: the .NET `android` workload (`dotnet workload install maui-android`) and the Android SDK (installed with Visual Studio, or `dotnet build -t:InstallAndroidDependencies`).

```powershell
# Run on a connected phone (USB debugging) or a started emulator
dotnet build src/ActivityTracker.Mobile -t:Run -f net10.0-android

# Build an installable APK -> artifacts/android/com.titimichon.activitytracker-Signed.apk
dotnet publish src/ActivityTracker.Mobile -c Release -f net10.0-android -o artifacts/android
```

To install the APK on a phone: copy it to the phone (or download it from a GitHub release), open it, and allow
"Install unknown apps" for the app you opened it with when Android asks.

The Mobile project is not in `ActivityTracker.sln` yet, so `validate.ps1` and CI do not need the Android workload.

## Project layout

```
src/
  ActivityTracker.Core/        Domain model and logic, no UI: schedules, goals, streaks, heatmap, persistence
  ActivityTracker.UI/          Shared UI (Razor class library): pages, components, layout, CSS/JS, app services.
                               Platform services (storage, file export, notifications) sit behind interfaces
                               in Platform/, with browser implementations; native hosts plug their own.
  ActivityTracker.Web/         Blazor WebAssembly PWA host (index.html, service worker, manifest)
  ActivityTracker.Mobile/      Android app host (.NET MAUI Blazor Hybrid): file storage, share sheet, notifications
tests/
  ActivityTracker.Core.Tests/  Unit tests (xUnit)
  ActivityTracker.Web.Tests/   Component tests (bUnit): UI behavior without a browser
  ActivityTracker.E2E/         End-to-end tests (Playwright + Chromium) against the published app
scripts/
  validate.ps1                 Full validation of a version (used locally and in CI)
  bump-version.ps1             Bumps the version and finalizes the changelog
  prepare-pages.ps1            Adapts the published site for GitHub Pages
.github/workflows/
  ci.yml                       On every push / PR: validate.ps1
  release.yml                  On tag vX.Y.Z: validate, GitHub release (zip), deploy to GitHub Pages
```

## Testing

```powershell
dotnet test                       # unit + component tests (E2E tests are skipped)
./scripts/validate.ps1            # everything, including publish and browser E2E tests
./scripts/validate.ps1 -SkipE2E   # quick check without browser
```

The first E2E run downloads Chromium (about 150 MB, cached afterwards).
To run only the E2E tests against an existing publish: set `E2E_APP_DIR` to `artifacts/publish/wwwroot` and run `dotnet test tests/ActivityTracker.E2E`.
Setting `SCREENSHOT_DIR` as well captures screenshots of every page with demo data.

## Versioning and releases

The version lives in **one place**: `<VersionPrefix>` in `Directory.Build.props` ([SemVer](https://semver.org/)). It is shown in the app (Settings page).

- `MAJOR`: incompatible change (e.g. data format that older versions can't read)
- `MINOR`: new feature
- `PATCH`: bug fix

Release flow:

```powershell
# 1. Describe the changes under "## [Unreleased]" in CHANGELOG.md (done in each PR)
# 2. Bump the version: moves Unreleased into a dated section, commits and tags
./scripts/bump-version.ps1 -Part minor -Commit
# 3. Push: the Release workflow validates, publishes the zip on GitHub Releases and deploys the site
git push origin main --follow-tags
```

The release pipeline refuses to publish if the tag does not match `Directory.Build.props`, if the changelog has no section for that version, or if any test fails. CI builds get a pre-release suffix (`1.0.0-ci.42`).

## One-time GitHub setup

1. Create an empty repository on GitHub, then:
   ```powershell
   git remote add origin https://github.com/<you>/<repo>.git
   git push -u origin main --follow-tags
   ```
2. **Settings → Pages → Source: GitHub Actions** (lets the release workflow deploy the app to `https://<you>.github.io/<repo>/`).
3. Recommended: **Settings → Branches → add a rule for `main`**: require the `Build & test` check to pass before merging.

## Data and privacy

There is no server. Everything is stored in your browser's local storage on each device. Clearing site data deletes it, so use *Settings → Export backup* regularly.
