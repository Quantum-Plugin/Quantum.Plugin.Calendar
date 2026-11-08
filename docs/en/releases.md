# Releases

[简体中文](../zh-CN/releases.md) · [Project guide](README.md)

## 5.0.1

- Move repeat and adjustment controls into the editor’s collapsed Advanced settings. The event and task switch appears when creating an entry. Remove the standalone rules panel and repeated help text. Adjustment options show the source name.
- Show events and tasks on adjacent-month dates in the month grid, with a subdued style. Monthly totals cover the selected month.
- Upgrade on a host running SDK `0.3.0-rc.3`. The release uses the 5.0.0 data schema and public contracts. If the update causes a problem, reinstall 5.0.0 on the same host with the existing data.
- See the [review and verification](https://github.com/Quantum-Plugin/Quantum.Plugin.Calendar/pull/6) and the [release record](https://github.com/Quantum-Plugin/Quantum.Plugin.Calendar/releases/tag/v5.0.1).

## 5.0.0

- Add neutral, version-1 recurrence-rules and date-adjustments extension points, independent per-entry opt-in, contributor lifecycle observation and desktop selection. Calendar includes no statutory, school or company policy. See [scheduling extensions](scheduling-extensions.md) for schemas and examples.
- Register all eight built-in repeat presets as ordinary contributions alongside external rules, with a single editor catalog and resolver. Preserve legacy stored fields through an adapter. Pattern presets and finite-date contributions share the version-1 contract.
- Add nullable `RepeatRuleId`/`AdjustmentRuleId` through migration 004. List results preserve nominal `OriginalDate` alongside actual `Date`; all existing entries retain their original behavior.
- **Breaking host compatibility:** require SDK `>=0.3.0-rc.3 <0.4.0`; rc.2 lacks the runtime extension API. Upgrade the host first. Build against the unchanged published rc.3 pin. Consumers editing extended events must preserve both new selectors and recognize `RepeatRuleId` as repetition even when `RepeatPattern=None`.
- Back up application data before upgrading. Migration 004 is additive: leave its columns in place on application rollback. After creating extended entries, restore the pre-upgrade backup when downgrading; older readers cannot calculate or safely edit those schedules. Do not modify released migrations or delete entries as a rollback shortcut.
- Validation includes real SQLite migration/round trips through generated clients, contributor lifecycle and malformed data, cross-window moves, cancellations, collision identity and UI rule preservation.

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
