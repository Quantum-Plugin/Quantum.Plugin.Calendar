# Releases

[简体中文](../zh-CN/releases.md) · [Project guide](README.md)

## 4.0.0

- Replace the legacy mixed mutation capabilities with `calendar.event.*` and `calendar.task.*`, separate resource request shapes and notifications. Use `calendar.entry.list/get` for mixed read operations and `calendar.mobile-page.refresh` for mobile snapshots. No old-name aliases remain. See the [capability migration guide](calendar-capabilities.md).
- Add repeating events: daily, Monday-Friday, selected weekdays, weekly/biweekly, monthly date/ordinal weekday/last day and yearly patterns, custom intervals, and date/count end conditions.
- Add a Monday-Sunday week view with overlapping events in separate columns, daily task strips, time-slot creation, cross-month/year navigation, and responsive scrolling. Preserve the month view and selected-day actions. See [calendar views](calendar-views.md).
- Apply additive migration `003_event_recurrence.sql`; existing IDs and contents remain unchanged and existing entries default to non-repeating.

### Compatibility and upgrade

Build against published `Quantum.Plugin.Abstraction` **0.3.0-rc.3** and publish with `--sdk-version-support ">=0.3.0-rc.2 <0.4.0"`. The supported host SDK range includes rc.2 and excludes 0.4.0; the build dependency remains exactly pinned to rc.3. This range identifies the host SDK rather than a Desktop application version. Use a verified CLI supporting that option (0.2.0 or later); CLI 0.1.1 does not support SDK compatibility options. Publish the same immutable archive verified by the release checks; do not repackage or overwrite an existing version.

Upgrade Calendar 4.0.0 and Ledger 0.7.0 together. Other consumers must migrate before relying on calendar integration. Xzzd calendar synchronization requires a separate consumer update and is not included in this release. Weekdays mean Monday-Friday without public-holiday adjustments. Repeating-event edits and deletion affect the entire series; single-occurrence exceptions are not supported.

Keep the normal application-data backup before upgrading. If integration failures require rollback, restore Calendar 3.0.0 together with Ledger 0.6.0 and the corresponding consumer versions. Leave migration 003's added columns in place; never delete recurring rows or rewrite released migrations to roll back the application. Old Calendar readers cannot expand newly created repeating series. Existing ledger associations continue to use series IDs.

No authentication or authorization mechanism is changed. Verification covers generated clients, recurrence boundaries, real SQLite migrations, resource ownership, week-view interactions and frontend scrolling. Run all checks in [Development](contributing.md). Signed release tags identify verified merged history; release notes link the PR, source commit, CI run and archive checksums.
