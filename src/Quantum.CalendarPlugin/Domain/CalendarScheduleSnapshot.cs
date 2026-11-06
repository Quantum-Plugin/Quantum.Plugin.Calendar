using System.Collections.Frozen;

namespace Quantum.OfficialPlugins.Calendar.Domain;

public sealed record CalendarScheduleSnapshot(IReadOnlyDictionary<string, CalendarRepeatRule> RepeatRules, IReadOnlyDictionary<string, CalendarDateAdjustment> Adjustments)
{
    public static CalendarScheduleSnapshot Empty { get; } = new(new Dictionary<string, CalendarRepeatRule>().ToFrozenDictionary(), new Dictionary<string, CalendarDateAdjustment>().ToFrozenDictionary());
}
