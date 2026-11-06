using NOF.Domain;

namespace Quantum.OfficialPlugins.Calendar.Domain;

public static class CalendarRecurrence
{
    public static void Validate(CalendarEntryKind kind, DateOnly date, CalendarRepeatPattern pattern, int interval, CalendarWeekdays weekdays, int? weekOfMonth, DateOnly? until, int? count)
    {
        if (!Enum.IsDefined(pattern) || interval is < 1 or > 999 || ((int)weekdays & ~127) != 0 || (weekOfMonth is not null && weekOfMonth is not (-1 or 1 or 2 or 3 or 4 or 5)) || count is < 1 or > 10000 || until < date || (until is not null && count is not null))
        {
            throw new DomainException("calendar_invalid_request", "The recurrence rule is invalid.");
        }

        if ((kind == CalendarEntryKind.Task && pattern != CalendarRepeatPattern.None) || (pattern != CalendarRepeatPattern.Weekly && weekdays != CalendarWeekdays.None) || (pattern != CalendarRepeatPattern.MonthlyWeekday && weekOfMonth is not null) || (pattern == CalendarRepeatPattern.Weekdays && interval != 1) || (pattern == CalendarRepeatPattern.None && (interval != 1 || until is not null || count is not null)))
        {
            throw new DomainException("calendar_invalid_request", "The recurrence options do not match the entry type or repeat pattern.");
        }

        if ((pattern == CalendarRepeatPattern.MonthlyLastDay && date.Day != DateTime.DaysInMonth(date.Year, date.Month)) || (pattern == CalendarRepeatPattern.Weekdays && date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) || (pattern == CalendarRepeatPattern.Weekly && weekdays != CalendarWeekdays.None && !weekdays.HasFlag((CalendarWeekdays)(1 << (int)date.DayOfWeek))) || (pattern == CalendarRepeatPattern.MonthlyWeekday && weekOfMonth is { } week && !MatchesMonthWeek(date, week)))
        {
            throw new DomainException("calendar_invalid_request", "The series start date must match the recurrence rule.");
        }
    }

    public static IEnumerable<DateOnly> Expand(CalendarEntry entry, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
        => Expand(entry.RepeatPattern, entry.Date, entry.RepeatInterval, entry.RepeatWeekdays, entry.RepeatWeekOfMonth, entry.RepeatUntil, entry.RepeatCount, start, end, cancellationToken);

    public static IEnumerable<DateOnly> Expand(CalendarRepeatPattern pattern, DateOnly anchor, int interval, CalendarWeekdays weekdays, int? weekOfMonth, DateOnly? until, int? occurrenceCount, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        var last = until is { } endDate && endDate < end ? endDate : end;
        var first = occurrenceCount is null && start > anchor ? start : anchor;
        var count = 0;
        for (var day = first.DayNumber; day <= last.DayNumber; day++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = DateOnly.FromDayNumber(day);
            if (!Matches(pattern, anchor, interval, weekdays, weekOfMonth, date))
            {
                continue;
            }

            count++;
            if (date >= start)
            {
                yield return date;
            }

            if (occurrenceCount is { } limit && count >= limit)
            {
                yield break;
            }
        }
    }

    private static bool Matches(CalendarRepeatPattern pattern, DateOnly anchor, int interval, CalendarWeekdays weekdays, int? weekOfMonth, DateOnly date)
    {
        var days = date.DayNumber - anchor.DayNumber;
        var months = (date.Year - anchor.Year) * 12 + date.Month - anchor.Month;
        return pattern switch
        {
            CalendarRepeatPattern.None => date == anchor,
            CalendarRepeatPattern.Daily => days % interval == 0,
            CalendarRepeatPattern.Weekdays => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
            CalendarRepeatPattern.Weekly => (days + ((int)anchor.DayOfWeek + 6) % 7) / 7 % interval == 0 && (weekdays == CalendarWeekdays.None ? date.DayOfWeek == anchor.DayOfWeek : weekdays.HasFlag((CalendarWeekdays)(1 << (int)date.DayOfWeek))),
            CalendarRepeatPattern.Monthly => months % interval == 0 && date.Day == anchor.Day,
            CalendarRepeatPattern.MonthlyLastDay => months % interval == 0 && date.Day == DateTime.DaysInMonth(date.Year, date.Month),
            CalendarRepeatPattern.MonthlyWeekday => months % interval == 0 && date.DayOfWeek == anchor.DayOfWeek && MatchesMonthWeek(date, weekOfMonth ?? (anchor.Day - 1) / 7 + 1),
            CalendarRepeatPattern.Yearly => (date.Year - anchor.Year) % interval == 0 && date.Month == anchor.Month && date.Day == anchor.Day,
            _ => false
        };
    }

    private static bool MatchesMonthWeek(DateOnly date, int week) => week == -1 ? date.Day + 7 > DateTime.DaysInMonth(date.Year, date.Month) : (date.Day - 1) / 7 + 1 == week;
}
