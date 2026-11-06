using Quantum.OfficialPlugins.Calendar.Application;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

internal sealed record WeekEventLayout
{
    public required CalendarEntryDetails Entry { get; init; }
    public required double StartMinute { get; init; }
    public required double EndMinute { get; init; }
    public required int Column { get; init; }
    public required int ColumnCount { get; init; }
    public string Style => FormattableString.Invariant($"--event-start: {StartMinute}; --event-duration: {EndMinute - StartMinute}; --event-column: {Column}; --event-columns: {ColumnCount}");
}
