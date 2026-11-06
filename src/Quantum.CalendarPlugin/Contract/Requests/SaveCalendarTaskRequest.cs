using Quantum.OfficialPlugins.Calendar.Domain;
using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed record SaveCalendarTaskRequest
{
    [Description("Calendar item title, up to 120 characters.")]
    public required string Title { get; set; }

    [Description("Calendar item notes, up to 1,000 characters.")]
    public required string Notes { get; set; }

    [Description("Calendar item date.")]
    public required DateOnly Date { get; set; }

    [Description("Task due time.")]
    public required TimeOnly StartTime { get; set; }

    [Description("Color identifier: violet, blue, coral, or green.")]
    public required string Style { get; set; }

    [Description("Optional date-adjustments contribution ID. Null preserves the original due date.")]
    public string? AdjustmentRuleId { get; set; }

    internal SaveCalendarEntryRequest ToEntry() => new()
    {
        Title = Title,
        Notes = Notes,
        Kind = CalendarEntryKind.Task,
        Date = Date,
        StartTime = StartTime,
        EndTime = null,
        Style = Style,
        AdjustmentRuleId = AdjustmentRuleId
    };
}
