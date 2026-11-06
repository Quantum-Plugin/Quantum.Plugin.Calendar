using System.Collections.Frozen;

namespace Quantum.OfficialPlugins.Calendar.Domain;

public static class CalendarBuiltInRecurrence
{
    public static IReadOnlyDictionary<string, CalendarRepeatRule> Rules { get; } = new[]
    {
        Rule("daily", "Every day", CalendarRepeatPattern.Daily, 0),
        Rule("weekdays", "Every weekday (Monday-Friday)", CalendarRepeatPattern.Weekdays, 1),
        Rule("weekly", "Every week", CalendarRepeatPattern.Weekly, 2),
        Rule("biweekly", "Every two weeks", CalendarRepeatPattern.Weekly, 3, 2),
        Rule("monthly", "Every month on the same date", CalendarRepeatPattern.Monthly, 4),
        Rule("monthly-weekday", "Every month on an ordinal weekday", CalendarRepeatPattern.MonthlyWeekday, 5),
        Rule("monthly-last-day", "Every month on the last day", CalendarRepeatPattern.MonthlyLastDay, 6),
        Rule("yearly", "Every year", CalendarRepeatPattern.Yearly, 7)
    }.ToFrozenDictionary(rule => rule.Id, StringComparer.Ordinal);

    public static CalendarScheduleSnapshot Snapshot { get; } = new(Rules, CalendarScheduleSnapshot.Empty.Adjustments);

    public static CalendarRepeatRule? FromLegacy(CalendarRepeatPattern pattern, int interval)
        => pattern == CalendarRepeatPattern.None ? null : Rules.Values.Single(rule => rule.Pattern == pattern && rule.Interval == (pattern == CalendarRepeatPattern.Weekly && interval == 2 ? 2 : 1));

    private static CalendarRepeatRule Rule(string id, string title, CalendarRepeatPattern pattern, int order, int interval = 1)
        => new($"quantum.plugin.calendar/builtin-{id}", title, Array.Empty<DateOnly>(), pattern, interval, order);
}
