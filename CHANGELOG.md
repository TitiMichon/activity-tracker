# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- **Today view** as the home screen: a to-do list for the day (unfinished items carry over to the next days), the activities scheduled today with their reminder time, and a long-term to-do list. Add, tick, rename, move between lists and delete to-dos right from Today. Other activities, today's log and objectives are folded at the bottom (#24).
- **Widgets on to-dos**, starting with a **checklist**: "Do the groceries" becomes the list of items to buy, with its progress (☑ 2/5) next to the to-do. Ticking every item ticks the to-do (can be turned off); untick all to reuse the list, remove ticked items, or remove the checklist (#25).
- **Android app** (.NET MAUI Blazor Hybrid) with the same screens as the web app: data stored in the app, backups exported through the Android share sheet, imported with the file picker, Android notification permission and notifications (#19).

- **Times per day** for planned activities (e.g. water 8× a day): each tap counts, tiles and the planned list show "3 / 8", the day is done (and the streak continues) only once the target is reached. The month view groups repeated entries into one badge with a count.
- **Hide from calendar** option per activity. Hidden activities still appear on Today, in the heatmap and when explicitly selected in the calendar filter.
- **Reminders**: a time per planned activity; if it is not done by then, a notification (when allowed) and an in-app message, once per day. Works while the app is open. Settings lists the reminders and asks for notification permission.

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
