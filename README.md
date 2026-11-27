# Activity Tracker

Track anything you do (music, sport, reading, mood, habits) on a calendar, with objectives and heatmaps.
It is a Blazor WebAssembly progressive web app: it runs entirely in the browser, works offline, can be installed on a phone or desktop, and keeps your data on your device.

What comes next is in the [roadmap](ROADMAP.md).

## Features (v1)

| | |
|---|---|
| **Activities** | Name, emoji logo, color. Three kinds: *Check* (done/not done), *Quantity* (value + unit, e.g. 30 min), *Scale* (options with their own icon/color/score, e.g. 😫 → 😄 mood). |
| **Periodicity** | Anytime, every day, chosen weekdays, every N days, monthly. Start date and optional deadline. Times per day (e.g. water 8×) with "3 / 8" progress. |
| **Reminders** | A time per planned activity: if not done by then, a notification. On Android it arrives even when the app is closed; on the web, while the app is open (tab or installed). |
| **Objectives** | "3× per week", "150 min per week", "500 pages until the deadline"… with progress bars. |
| **Today** | One list for the day: tasks planned or due today, overdue ones and unfinished ones from earlier days, then the activities planned today. Type a task and press Enter to add it; tick, rename, delete (with undo). Ticking an activity logs it; Quantity/Scale open a mini picker. |
| **Backlog** | Tasks for no particular day (repair the car, change the window…), sorted by due date with a "due in 3 days" badge. Plan one for today or another day; send a task of today back. A tab next to Today on phones, the right column on wide screens. |
| **Updates (Android)** | The app offers new versions and installs them in one tap: releases only, or every change merged (dev builds). |
| **Quick add** | Other activities, objectives and today's log are one tap away under "More for today". Undo in the toast. |
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

# Build an installable APK -> artifacts/android/activity-tracker-v<version>.apk
./scripts/build-android.ps1
```

Locally the APK is signed with your debug key unless `ANDROID_KEYSTORE_PATH` and `ANDROID_KEYSTORE_PASSWORD` point to the
release key. Published APKs are all **release-signed**, so each one installs over the previous one:

| Build | When | Version | Where |
|---|---|---|---|
| Release | version tag `vX.Y.Z` | `1.2.0` | [GitHub release](https://github.com/TitiMichon/activity-tracker/releases/latest) |
| Dev build | every merge to `main` | `1.1.1-dev.42`: the next patch, so newer than the last release and older than the next one | GitHub **pre-release**; only the last 5 are kept |
| Check | every pull request | `1.1.0-ci.42`, debug key | workflow artifact (not installable over the others) |

#### Install on your phone

1. Open the [latest release](https://github.com/TitiMichon/activity-tracker/releases/latest) on the phone and download `activity-tracker-vX.Y.Z.apk`
   (or the newest **Pre-release** on the [releases page](https://github.com/TitiMichon/activity-tracker/releases) for the latest changes).
2. Open it and allow "Install unknown apps" for your browser / file manager when Android asks.

Updates install over the previous version and keep your data, as long as both are signed with the release key.
**Coming from an APK built on a PC** (debug key): Android refuses that update. Export a backup in Settings, uninstall
the app, install the release APK, then import the backup.

#### Updates from inside the app

The Android app looks for a new version when it starts (at most every 6 hours) and shows **Update available**. **Update**
downloads the APK and opens Android's installer; your data is kept. The first time, Android asks you to allow Activity
Tracker to install apps: allow it, go back and tap **Update** again. Play Protect may also offer to scan the app, which is
normal for apps that don't come from the Play Store.

**Settings → Updates**: installed version, **Check now**, and what to get: *Releases only* or *Every change* (the dev
build of every merged change). Dev builds follow every change by default, releases stay on releases.
Android only installs a download signed with the same release key as the installed app.

#### Get notified of new versions (Obtainium)

Optional now that the app updates itself.

[Obtainium](https://github.com/ImranR98/Obtainium) watches the GitHub releases and installs updates in one tap
(Android still asks to confirm each install of an app that doesn't come from the Play Store).

1. Install Obtainium from its [releases](https://github.com/ImranR98/Obtainium/releases) or from F-Droid.
2. **Add app** → `https://github.com/TitiMichon/activity-tracker` → Add (no token needed, the repository is public).
   To get every dev build too, turn on **Include prereleases**. If Activity Tracker is already installed from a
   release APK, Obtainium recognizes it; otherwise tap **Install**.

Obtainium checks for updates in the background; tap **Update** when a new version shows up. Your data is kept.

The Mobile project is not in `ActivityTracker.sln`, so `validate.ps1` does not need the Android workload.

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
  build-android.ps1            Builds the Android APK (release key when the signing secrets are set)
  prepare-pages.ps1            Adapts the published site for GitHub Pages
.github/workflows/
  ci.yml                       On every push / PR: validate.ps1 + Android APK build; on main, publishes the
                               release-signed APK as a dev pre-release (keeps the last 5)
  changelog.yml                On PRs: requires a CHANGELOG entry for src/ changes (label skip-changelog to bypass)
  release.yml                  On tag vX.Y.Z (or run by hand): validate, GitHub release with web zip + signed APK,
                               optional GitHub Pages deploy
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
# 3. Push: the Release workflow validates, then publishes the web zip and the signed APK on GitHub Releases
git push origin main --follow-tags
```

If a release run failed or a tag was pushed before the workflow existed: **Actions → Release → Run workflow** with the tag.

The release pipeline refuses to publish if the tag does not match `Directory.Build.props`, if the changelog has no section for that version, or if any test fails. Pull request builds get a `-ci.N` suffix; builds of `main` are published as `<next patch>-dev.N` pre-releases (see [Android app](#android-app)).

## One-time GitHub setup

1. Create an empty repository on GitHub, then:
   ```powershell
   git remote add origin https://github.com/<you>/<repo>.git
   git push -u origin main --follow-tags
   ```
2. **Android signing key** (once, keep a backup outside GitHub: every update must use the same key):
   ```powershell
   keytool -genkeypair -keystore activity-tracker-release.jks -storetype PKCS12 -alias activity-tracker `
           -keyalg RSA -keysize 4096 -validity 10000 -dname "CN=Activity Tracker"
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("activity-tracker-release.jks")) | gh secret set ANDROID_KEYSTORE_BASE64
   gh secret set ANDROID_KEYSTORE_PASSWORD      # the password chosen above
   ```
3. Optional, **GitHub Pages** (public repositories or paid plans): Settings → Pages → Source: GitHub Actions, then set the
   repository variable `DEPLOY_PAGES` to `true` so releases also deploy the app to `https://<you>.github.io/<repo>/`.
3. Recommended: **Settings → Branches → add a rule for `main`**: require the `Build & test` check to pass before merging.

## Data and privacy

There is no server. Everything is stored in your browser's local storage on each device. Clearing site data deletes it, so use *Settings → Export backup* regularly.
