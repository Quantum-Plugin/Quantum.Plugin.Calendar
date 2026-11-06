# Calendar capabilities and repeating events

[简体中文](../zh-CN/calendar-capabilities.md) · [Architecture](architecture.md)

Version 4 removes the old `calendar.create`, `calendar.update`, `calendar.delete`, `calendar.get`, `calendar.list`, `calendar.list-tasks`, `calendar.set-completed`, `calendar.mobile-refresh` and `calendar.created/updated/deleted` capability names. There are no compatibility aliases. Install coordinated consumer updates when upgrading; plugin IDs, stored entry IDs, routes and existing migration files remain stable.

## Resource names

| Resource | Request capabilities |
| --- | --- |
| Event | `calendar.event.create`, `.get`, `.list`, `.update`, `.delete` |
| Task | `calendar.task.create`, `.get`, `.list`, `.update`, `.delete`, `.set-completed` |
| Mixed calendar entry reads | `calendar.entry.get`, `calendar.entry.list` |
| Mobile page | `calendar.mobile-page.refresh` |

Each event and task publishes its own `calendar.event.created/updated/deleted` or `calendar.task.created/updated/deleted` notification, containing `id`. Notifications follow a successful database commit. Read dispatch uses `All`; mutations use `Ordered` with unique ownership. Creation and mobile refresh require a single provider. An ID belonging to the other resource is treated as absent, never converted or mutated. Only tasks support completion.

The [RPC](glossary.md#rpc) source of truth is [ICalendarRpcService](../../src/Quantum.CalendarPlugin/Contract/ICalendarRpcService.cs). Event and task save requests have separate shapes and omit `kind`. Task saves also omit end times and repeat fields. `calendar.task.list` accepts `includeCompleted` (default true); event and mixed entry lists accept inclusive `startDate` and `endDate`. `kind` applies only to mixed entry lists.

## Repeating events

All repeat fields are optional; omission creates a single event. Dates use `yyyy-MM-dd`, times use `HH:mm:ss`, and IDs use UUID strings. Events retain the plugin's local wall-clock date/time model; recurrence does not convert times across time zones.

| `repeatPattern` | Behavior |
| --- | --- |
| `None` | One event, the default |
| `Daily` | Every `repeatInterval` days |
| `Weekdays` | Every Monday-Friday; interval must be 1; no public-holiday or make-up-workday calendar |
| `Weekly` | Every N Monday-based weeks on selected weekdays; interval 2 is biweekly |
| `Monthly` | Every N months on the start day of month |
| `MonthlyWeekday` | Every N months on the start weekday and ordinal; `repeatWeekOfMonth` can be 1-5 or -1 for last; omission uses the start date's ordinal |
| `MonthlyLastDay` | Every N months on the last calendar day |
| `Yearly` | Every N years on the start month/day |

`repeatInterval` defaults to 1 and accepts 1-999. Monthly interval 3 gives quarterly events; interval 6 gives half-yearly events. Weekly `repeatWeekdays` is a flags value: Sunday=1, Monday=2, Tuesday=4, Wednesday=8, Thursday=16, Friday=32, Saturday=64. Combine values (Monday/Wednesday/Friday = 42), or use the string flag names such as `"Monday, Wednesday, Friday"`. Omission/None uses the start weekday. The series start must match the rule. Dates that do not exist, such as February 31 or February 29 in a non-leap year, are skipped and do not consume an occurrence count; dates never drift to the end of a shorter month.

Choose either an inclusive `repeatUntil` date or `repeatCount` (1-10000, including the series start). Omit both for no end. The two limits cannot be combined. Fields that do not apply to the selected pattern are rejected.

Example payload for `calendar.event.create`:

```json
{
  "title": "Team sync",
  "notes": "Planning",
  "date": "2026-11-06",
  "startTime": "09:00:00",
  "endTime": "09:30:00",
  "style": "blue",
  "repeatPattern": "Weekly",
  "repeatInterval": 2,
  "repeatWeekdays": "Monday, Friday",
  "repeatCount": 10
}
```

List queries expand occurrences in the requested window, including series that started earlier. Each result keeps the series `id`, reports the occurrence in `date`, and exposes the anchor in `seriesStartDate`. Get/create/update results report the series start in `date`. Editing from any occurrence opens the series start and its complete rule. Updates and deletion affect the entire series; individual-occurrence exceptions are not supported. Linked ledger transactions remain attached to the series ID.

A list request accepts at most 3660 inclusive days and returns at most 10000 occurrences; larger results fail without truncation and should be queried in smaller windows. Invalid rules/ranges use `calendar_invalid_request`. Missing or wrong-kind IDs use `calendar_entry_not_found` locally and pass ownership to the next provider during host dispatch. Cancellation follows the host request pipeline. Authentication, permissions, rate limits and timeouts are enforced by the host.

## Storage upgrade

The host applies `003_event_recurrence.sql` after migrations 001 and 002. It only adds repeat columns with non-repeating defaults, preserving existing IDs and contents. The [migration](glossary.md#migration) is additive and contains no backfill or destructive cleanup. Retain the normal host database backup before upgrading. To revert the application, restore the previous plugin; its existing-column queries ignore the added fields, but it cannot expand newly created series. Keep the database columns until a separately reviewed data migration.
