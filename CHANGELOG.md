# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- **Dev builds**: every change merged to `main` is published within minutes as a release-signed Android APK (a GitHub pre-release such as `1.1.1-dev.42`), installable over the current version (#59).
- **Today is one list for the day**: tasks planned or due today, overdue tasks (in red) and unfinished tasks from earlier days ("since Tue 24 Nov"), then the activities planned today with their reminder time. Type a task and press Enter to add it; tick, rename or delete it (with undo). Tapping an activity's round button logs it ("2/8" for several times a day, a mini picker for Quantity and Scale); tapping again unticks a done check-in. Other activities, objectives and today's log moved to the folded "More for today" section (#31).

### Changed

- **Data format v2** to store tasks. Your data is upgraded automatically the first time v1.2 opens, and backups from v1.0 and v1.1 still import. Backups made with v1.2 need v1.2 or later to import (#30).

## [1.1.0] - 2026-11-26

### Added

- **Android app** (.NET MAUI Blazor Hybrid) with the same screens as the web app: data stored in the app, backups exported through the Android share sheet, imported with the file picker, Android notifications (#19).
- **Android APK on every release**, signed with the same release key so new versions install over the previous one and keep your data; update notifications with Obtainium (#21, #28).
- **Reminders**: a time per planned activity; if it is not done by then, a notification (when allowed) and an in-app message, once per day. Settings lists the reminders and asks for notification permission. On Android they arrive even when the app is closed (an alarm wakes the app; set again after a reboot, an update or a time zone change; can be a few minutes late while the phone is idle). On the web they work while the app is open (#20).
- **Times per day** for planned activities (e.g. water 8× a day): each tap counts, tiles and the planned list show "3 / 8", the day is done (and the streak continues) only once the target is reached. The month view groups repeated entries into one badge with a count.
- **Hide from calendar** option per activity. Hidden activities still appear on Today, in the heatmap and when explicitly selected in the calendar filter.

### Changed

- Heatmap days take the color of the last entry of the day, including its custom color, instead of the option closest to the day's average score.

### Fixed

- Time fields (reminder time, entry time) no longer snap back to their previous value in the browser (#9).

## [1.0.0] - 2026-11-16

### Added

- Activities with a name, emoji logo, color and one of three kinds: **Check** (done / not done), **Quantity** (value + unit) and **Scale** (options with their own icon, color and score, e.g. mood).
- Templates: music practice, sport, reading, mood, meditation, water.
- Periodicity: anytime, daily, selected weekdays, every N days, monthly; start date and optional deadline.
- Objectives per day / week / month / year / total-until-deadline, counted in times or summed values, with progress bars.
- Quick add from the Today page: one tap for Check activities, value or option picker for the others, with Undo.
- Per-entry custom icon and color overriding the option or activity defaults.
- Calendar with day, week and month views, activity filter and "planned but not done" markers.
- GitHub-style heatmap for all activities, one activity, or one per activity, with streaks and totals.
- Settings: first day of the week, JSON export / import, delete all data.
- Installable offline PWA; data stored locally in the browser.
- Unit tests (xUnit), component tests (bUnit), end-to-end tests (Playwright), `scripts/validate.ps1`, CI and release pipelines with GitHub Pages deployment.

[Unreleased]: ../../compare/v1.0.0...HEAD
[1.0.0]: ../../releases/tag/v1.0.0
