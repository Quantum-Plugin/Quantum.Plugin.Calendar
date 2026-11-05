namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed record CalendarEntryChanged
{
    public required Guid Id { get; init; }
}
