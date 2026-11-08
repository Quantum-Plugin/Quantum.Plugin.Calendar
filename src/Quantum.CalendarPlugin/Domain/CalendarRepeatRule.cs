namespace Quantum.OfficialPlugins.Calendar.Domain;

public sealed record CalendarRepeatRule(string Id, string Title, IReadOnlyList<DateOnly> Dates, CalendarRepeatPattern? Pattern = null, int Interval = 1, int Order = 0);
