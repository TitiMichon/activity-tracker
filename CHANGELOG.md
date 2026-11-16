# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
