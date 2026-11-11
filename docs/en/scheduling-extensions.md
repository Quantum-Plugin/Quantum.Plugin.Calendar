# Scheduling extensions

[简体中文](../zh-CN/scheduling-extensions.md) · [Capabilities](calendar-capabilities.md) · [Architecture](architecture.md)

Calendar owns dates, entry identity, validation and occurrence expansion. It contains no statutory, regional, school, company or institution-specific scheduling policy. Optional plugins contribute selectable rules to [extension points](glossary.md#extension-point) using [JSON](glossary.md#json) data through the published [SDK](glossary.md#sdk) `IQuantumExtensions` API. No sibling source references, shared plugin assembly, new manifest contribution fields, network lookup or business-code callbacks are involved.

## Contract version 1

| Extension point | Payload schema | Meaning |
| --- | --- | --- |
| `quantum.plugin.calendar/recurrence-rules` | [Recurrence schema](../contracts/recurrence-rules.v1.schema.json) | A pattern preset or a finite set of nominal dates, for example the Mondays of selected teaching weeks. |
| `quantum.plugin.calendar/date-adjustments` | [Adjustment schema](../contracts/date-adjustments.v1.schema.json) | A bounded mapping from nominal dates to actual dates, or null to omit an occurrence. |

The full extension-point IDs include the owning plugin namespace: `quantum.plugin.calendar/recurrence-rules` and `quantum.plugin.calendar/date-adjustments`. `Define("recurrence-rules", 1)` takes a local name because the SDK prefixes it with the current plugin's host-attributed ID; `Register` and `Observe` always take the full point ID. Another plugin defining the same local name owns a different point. Do not persist or resolve a bare local name, or add the namespace a second time when calling `Define`.

The host attributes each contribution with `PluginId` and `LocalId`; the persisted selection is their exact, case-sensitive `plugin-id/local-id` combination. Payloads cannot override ownership. For example, `example.school/term` and `example.company/term` can coexist; selecting or withdrawing one never selects or removes the other, even if their titles and local IDs match. A payload member such as `id` or `pluginId` cannot replace the host-attributed identity. Keep IDs stable when updating a term or refreshing a source. Titles are nonblank, at most 120 characters, and displayed as text alongside identity. Dates use `yyyy-MM-dd`, without time zones. Unknown object members are ignored for forward compatibility. Definitions use exact contract version 1; other versions do not participate.

A recurrence payload selects exactly one of two shapes. A `pattern` payload declares `Daily`, `Weekdays`, `Weekly`, `Monthly`, `MonthlyWeekday`, `MonthlyLastDay` or `Yearly`, with an optional default `interval` (1–999, default 1; Weekdays requires 1). It cannot contain `dates`. A finite-date payload contains unique `dates` (1–10,000), cannot contain `pattern` or `interval`, and is accepted in any order and sorted by Calendar. The finite-date shape describes a complete finite pattern. The contributor computes teaching weeks, term boundaries, odd/even weeks and other policy into separate selectable date sets; Calendar does not interpret those concepts. The actual SDK payload limit (128 KiB) still applies and can be reached before the count limit. Empty date sets are invalid; withdraw a contribution when it no longer exists. A selected finite pattern never invents dates beyond its published set, even if the editor's end condition is Never. Pattern presets use the ordinary interval, weekday, ordinal and end-condition fields saved on the entry; a descriptor's interval is an editor default, not an override of existing saved parameters.

Calendar registers its own built-in presets through the same `IQuantumExtensions.Register` method and contract version; observers see them as contributions from `quantum.plugin.calendar`. Their stable local IDs are `builtin-daily`, `builtin-weekdays`, `builtin-weekly`, `builtin-biweekly`, `builtin-monthly`, `builtin-monthly-weekday`, `builtin-monthly-last-day` and `builtin-yearly`. Biweekly uses `pattern=Weekly`, `interval=2`; the others default to interval 1. The editor discovers one catalog for all rules, follows the host-attributed contribution order, and localizes Calendar's own titles. Does not repeat means no selected rule, rather than a separate recurrence provider.

For example, a built-in or external pattern contribution uses this same payload:

```json
{"title":"Every two weeks","pattern":"Weekly","interval":2}
```

An adjustment payload requires `validFrom <= validUntil` and up to 10,000 `changes`. Every source and non-null target must lie inside that inclusive coverage. Each source appears at most once. An unspecified source date inside coverage stays unchanged. Outside coverage is unknown and fails explicitly. Null omits that occurrence; moving one date to another does not automatically exchange the two. For a swap, contribute both mappings. Mappings apply once, simultaneously; destinations are never fed back through the source map.

For example, a companion plugin can hold these registration handles until stop:

```csharp
using var repeat = JsonDocument.Parse("""
    {"title":"Example teaching Fridays", "dates":["2026-11-06","2026-11-20","2026-12-04"]}
    """);
var term = extensions.Register("quantum.plugin.calendar/recurrence-rules", 1,
    "teaching-fridays", repeat.RootElement);
using var adjustment = JsonDocument.Parse("""
    {"title":"Example company calendar", "validFrom":"2026-01-01", "validUntil":"2026-12-31",
     "changes":[{"date":"2026-11-06","targetDate":"2026-12-01"},
                {"date":"2026-11-07","targetDate":null}]}
    """);
var shifts = extensions.Register("quantum.plugin.calendar/date-adjustments", 1,
    "working-days", adjustment.RootElement);
// On stop: term.Dispose(); shifts.Dispose();
```

Use `IQuantumContribution.Update` to replace a complete payload, and dispose to withdraw it. Registration and updates follow the SDK's runtime catalog lifecycle. Calendar takes an immutable snapshot for each operation; later updates affect subsequent reads. The desktop observes changes and refreshes its choices and occurrences on the UI thread; the mobile snapshot follows its existing refresh loop. Stop disposes definitions and observations; restart observes afresh. Invalid contributions are logged and ignored individually without hiding valid siblings. Extension data is host-session state; contributors own durable policy data, provenance and refreshes. Do not contribute secrets.

## Per-entry choices and [occurrence](glossary.md#occurrence) evaluation

Existing entries and new entries default to `RepeatRuleId = null` and `AdjustmentRuleId = null`. Nothing automatically opts an entry into any source. Events select a contributed `RepeatRuleId` for both built-in and external rules. For a pattern-backed contribution, set `RepeatPattern` to its declared pattern and copy its default interval when initially selecting it; the user may customize the saved interval and applicable weekday/ordinal fields. The saved pattern must continue to match the contribution, so a provider cannot silently change the engine under existing entries. For a finite-date rule, use `RepeatPattern=None`, interval 1, no weekday flags and no ordinal; the anchor must occur in the contributed date set. Date/count end conditions remain available. Legacy entries with no `RepeatRuleId` are adapted to the corresponding built-in descriptor during resolution, without changing stored fields or requiring a data migration. Editing such an entry preserves the old representation until a rule is explicitly selected.

Events and tasks can independently select one `AdjustmentRuleId`, or choose Keep original dates. There is no global adjustment switch, implicit source selection, source priority or stacking. A school/plugin can publish a combined policy if needed; Calendar does not decide which organization's policy wins. Tasks remain non-repeating. A task mapped to null is omitted from expanded task lists until the policy changes or its adjustment selection is repaired through direct get/update.

Evaluation order is: generate nominal dates at or after the anchor; apply the inclusive until limit or count limit; apply the selected adjustment once; filter by actual query dates; sort. Skipped occurrences still consume their original count. An adjusted occurrence can appear after RepeatUntil. Queries include mappings whose source lies outside the query and whose target lies inside it. Multiple nominal occurrences moved to the same date remain separate, identified by `(Id, OriginalDate)`; Calendar never merges them or changes entry IDs, time-of-day or completion state.

List results expose actual `Date`, nominal `OriginalDate` and original `SeriesStartDate`. Direct get/create/update return the stored anchor and full selection, without requiring available contributors for get/delete. Editing any occurrence uses the anchor and preserves both selections; changes/deletion affect the entire series. Single-occurrence overrides remain outside this contract.

## Failure and compatibility

Selections are never silently replaced when a provider is disabled, its contribution is malformed or coverage expires. Writes with an unavailable selection fail with `calendar_schedule_unavailable`; anchor or option violations use `calendar_invalid_request`. Expansion of an affected series fails with `calendar_schedule_unavailable` or `calendar_adjustment_out_of_range`. The entire requested list fails to avoid showing incomplete schedules. Other lists that do not select that series can still succeed.

Open **Advanced settings** at the bottom of the entry editor to clear or replace a selection. The editor retains unavailable source IDs on already loaded entries. If the entry is absent from the calendar, restore or update the provider, or use the direct event/task get and update APIs. To find the stored entry, call `calendar.entry.list` with `SeriesPage`. A non-null zero-based page (0–100000) returns up to 100 stored series, including entries omitted by an adjustment, ordered by anchor date then ID without evaluating contributions. The date bounds filter stored anchors and may span the full date domain. Null uses normal occurrence expansion with its 3,660-day limit.

Extension callbacks do no external requests. Existing request cancellation, inclusive query bounds, 3,660-day range limit and 10,000 returned-occurrence limit continue to apply. These are host-local contributions, not a new authorization boundary. Existing capability names and request generation are unchanged.

Calendar 5.0.0 requires SDK `>=0.3.0-rc.3 <0.4.0`, because rc.2 lacks the extension API. Upgrade the host before installing it. Migration `004_scheduling_extensions.sql` adds nullable selection columns without rewriting existing data or old migrations. Build clients must account for `RepeatRuleId` as well as `RepeatPattern` when identifying repeating events and preserve the saved engine parameters for pattern-backed contributions. Older consumers ignoring the new fields cannot faithfully edit an extended schedule. Restore the previous application-data backup if downgrading after extended entries were created; old readers cannot evaluate those choices. See [releases](releases.md).

Verification covers isolated payload parsing/lifecycle, immutable operation snapshots, missing sources, domain invariants, count/coverage boundaries, cancellation, cross-window moves, generated clients, real SQLite upgrade/persistence and editor round trips. Host-to-host integration with actual statutory, company or teaching-calendar plugins belongs in the consuming host and is not supplied by Calendar.
