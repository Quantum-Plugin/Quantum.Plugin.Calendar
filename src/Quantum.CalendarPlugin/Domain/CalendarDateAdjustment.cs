namespace Quantum.OfficialPlugins.Calendar.Domain;

public sealed record CalendarDateAdjustment(string Id, string Title, DateOnly ValidFrom, DateOnly ValidUntil, IReadOnlyDictionary<DateOnly, DateOnly?> Changes);
