using Quantum.OfficialPlugins.Calendar.Domain;
using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed record CalendarEntryDetails
{
    [Description("Calendar item ID.")]
    public required Guid Id { get; set; }

    [Description("Calendar item title.")]
    public required string Title { get; set; }

    [Description("Calendar item notes.")]
    public required string Notes { get; set; }

    [Description("Item kind: Event or Task.")]
    public required CalendarEntryKind Kind { get; set; }

    [Description("Occurrence date in list results; series start in get/create/update results.")]
    public required DateOnly Date { get; set; }

    [Description("Event start time or task due time.")]
    public required TimeOnly StartTime { get; set; }

    [Description("Event end time; null for tasks.")]
    public required TimeOnly? EndTime { get; set; }

    [Description("Whether the task is completed.")]
    public required bool IsCompleted { get; set; }

    [Description("UI color identifier.")]
    public required string Style { get; set; }

    [Description("Repeat pattern: None, Daily, Weekdays (Monday-Friday), Weekly, Monthly (same date), MonthlyWeekday (same ordinal weekday), MonthlyLastDay, or Yearly. Events only.")]
    public CalendarRepeatPattern RepeatPattern { get; set; }

    [Description("Repeat every N days/weeks/months/years, 1-999. Use Weekly with 2 for biweekly. Weekdays requires 1.")]
    public int RepeatInterval { get; set; } = 1;

    [Description("Weekly weekday flags: Sunday=1, Monday=2, Tuesday=4, Wednesday=8, Thursday=16, Friday=32, Saturday=64. None uses the series start weekday.")]
    public CalendarWeekdays RepeatWeekdays { get; set; }

    [Description("MonthlyWeekday ordinal: 1-5, or -1 for last. Null uses the series start ordinal.")]
    public int? RepeatWeekOfMonth { get; set; }

    [Description("Inclusive last date; null means no date limit. Cannot combine with RepeatCount.")]
    public DateOnly? RepeatUntil { get; set; }

    [Description("Total occurrences including the series start, 1-10000; null means no count limit. Cannot combine with RepeatUntil.")]
    public int? RepeatCount { get; set; }

    [Description("Original series start date, even when Date is an expanded occurrence date.")]
    public DateOnly SeriesStartDate { get; set; }
}
