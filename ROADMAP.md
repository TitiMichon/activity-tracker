# Roadmap

Activity Tracker is a **phone-first** personal organizer: plan the day, keep a backlog, follow recurring activities and the things that matter (mood, water, tobacco...). Version 1 runs on one phone (Android) and in the browser; syncing devices comes later.

Each version below is a [GitHub milestone](https://github.com/TitiMichon/activity-tracker/milestones) with one issue per feature.

## Versioning

- **MINOR (1.x.0)**: a milestone of features. Data is upgraded automatically; backups from earlier 1.x versions still import.
- **PATCH (1.x.y)**: fixes and small tweaks from daily use, between milestones.
- **MAJOR (2.0.0)**: a change of foundation, such as sync between devices.

Every release is tagged, described in [CHANGELOG.md](CHANGELOG.md) and ships the web app plus a signed Android APK.

## Versions

### v1.1.0: Android release

Everything already on `main` (hide from calendar, reminders, several check-ins a day, scale colors, Android app), plus:

- Signed APK built by CI and attached to each release.
- Update notifications on the phone through Obtainium.
- Reminders that fire when the app is closed.

### v1.2.0: Tasks and the new Today

The core of daily use. A **task** is done once; an **activity** is recurring and logged.

- Task model: title, notes, planned day, due date, subtasks, optional activity. Automatic data upgrade.
- Today: one list with the tasks planned or due today (and the ones left from earlier days) and the activities scheduled today.
- Backlog of unplanned tasks, sorted by due date, one swipe away.
- Subtasks with a 0/n indicator (Cleanup → windows, floor).
- Tracker row: mood, water, tobacco... tap to log, long-press for a note.
- Limit goals: "at most 5 a day".

### v1.3.0: Activities page

- Recurring activities with their next occurrences; an icon switches to the calendar.
- Activity roadmap: tasks linked to an activity toward a long-term goal (Music → read notes, learn a partition...).
- Navigation: **Today · Activities · Stats**, settings behind an icon.

### v1.4.0: Stats

- Favorite and reorder heatmaps.
- Task heatmap.
- Weekday × hour heatmap ("when do I do it?").
- Activity groups.

### v1.5.0: History and safety

- Change history and restore points.
- Automatic backups (a phone-only app must survive a lost phone).

### v1.6.0: Gamification

- XP and levels, computed from the history, trust-based.
- Coins and a cosmetics shop (themes, icon packs).

### Later

- Google Play (internal testing, then public): real automatic updates.
- Sync between devices (2.0.0): QR pairing, encrypted changes through a relay.
- iOS app.

## Why this order

1. **v1.1.0** puts the app on the phone the right way (installed from releases, updates notified), so daily use and feedback start now.
2. **v1.2.0** is what makes the app usable every day; it changes the data model, so it comes before the screens built on top of it.
3. **v1.3.0 and v1.4.0** reorganize existing screens around that model.
4. **v1.5.0** before **v1.6.0**: XP is computed from the history, and the history is also what a future sync builds on.
