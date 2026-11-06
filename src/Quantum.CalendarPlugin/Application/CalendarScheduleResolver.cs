using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.OfficialPlugins.Calendar.Application;

public static class CalendarScheduleResolver
{
    public static void Validate(CalendarEntryDetails item, CalendarScheduleSnapshot snapshot)
    {
        var rule = ResolveRepeatRule(item, snapshot);
        if (rule is not null)
        {
            ValidateOptions(item, rule);
            if (rule.Pattern is null && !rule.Dates.Contains(item.Date))
            {
                throw new DomainException("calendar_invalid_request", "The series start date must match the contributed repeat rule.");
            }
        }

        if (item.AdjustmentRuleId is { } adjustmentId)
        {
            Adjust(item.Date, Adjustment(snapshot, adjustmentId));
        }
    }

    public static IEnumerable<CalendarEntryDetails> Expand(CalendarEntryDetails item, CalendarScheduleSnapshot snapshot, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var adjustment = item.AdjustmentRuleId is { } adjustmentId ? Adjustment(snapshot, adjustmentId) : null;
        var first = start;
        var last = end;
        if (adjustment is not null)
        {
            // Include nominal dates outside the query whose destinations fall inside it.
            foreach (var change in adjustment.Changes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (change.Value >= start && change.Value <= end)
                {
                    first = change.Key < first ? change.Key : first;
                    last = change.Key > last ? change.Key : last;
                }
            }
        }

        IEnumerable<DateOnly> dates;
        var rule = ResolveRepeatRule(item, snapshot);
        if (rule is not null)
        {
            ValidateOptions(item, rule);
        }

        if (rule is { Pattern: null })
        {
            dates = rule.Dates.Where(date => date >= item.Date && (item.RepeatUntil is null || date <= item.RepeatUntil));
            if (item.RepeatCount is { } count)
            {
                dates = dates.Take(count);
            }

            dates = dates.Where(date => date >= first && date <= last);
        }
        else
        {
            dates = CalendarRecurrence.Expand(rule?.Pattern ?? CalendarRepeatPattern.None, item.Date, item.RepeatInterval, item.RepeatWeekdays, item.RepeatWeekOfMonth, item.RepeatUntil, item.RepeatCount, first, last, cancellationToken);
        }

        foreach (var date in dates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actual = adjustment is null ? date : Adjust(date, adjustment);
            if (actual is { } destination && destination >= start && destination <= end)
            {
                yield return item with { Date = destination, OriginalDate = date, SeriesStartDate = item.Date };
            }
        }
    }

    private static CalendarRepeatRule? ResolveRepeatRule(CalendarEntryDetails item, CalendarScheduleSnapshot snapshot)
        => item.RepeatRuleId is { } id ? RepeatRule(snapshot, id) : CalendarBuiltInRecurrence.FromLegacy(item.RepeatPattern, item.RepeatInterval);

    private static void ValidateOptions(CalendarEntryDetails item, CalendarRepeatRule rule)
    {
        if (item.RepeatPattern != (rule.Pattern ?? CalendarRepeatPattern.None))
        {
            throw new DomainException("calendar_invalid_request", "The recurrence options do not match the selected repeat rule.");
        }

        CalendarRecurrence.Validate(item.Kind, item.Date, item.RepeatPattern, item.RepeatInterval, item.RepeatWeekdays, item.RepeatWeekOfMonth, item.RepeatUntil, item.RepeatCount, item.RepeatRuleId);
    }

    private static CalendarRepeatRule RepeatRule(CalendarScheduleSnapshot snapshot, string id) => snapshot.RepeatRules.TryGetValue(id, out var rule) ? rule : throw new DomainException("calendar_schedule_unavailable", "A selected scheduling rule is unavailable. Restore its provider or edit the entry's scheduling options.");
    private static CalendarDateAdjustment Adjustment(CalendarScheduleSnapshot snapshot, string id) => snapshot.Adjustments.TryGetValue(id, out var rule) ? rule : throw new DomainException("calendar_schedule_unavailable", "A selected scheduling rule is unavailable. Restore its provider or edit the entry's scheduling options.");
    private static DateOnly? Adjust(DateOnly date, CalendarDateAdjustment adjustment)
    {
        if (date < adjustment.ValidFrom || date > adjustment.ValidUntil)
        {
            throw new DomainException("calendar_adjustment_out_of_range", "The selected adjustment source does not cover this date. Update its provider or edit the entry's scheduling options.");
        }

        return adjustment.Changes.TryGetValue(date, out var target) ? target : date;
    }
}
