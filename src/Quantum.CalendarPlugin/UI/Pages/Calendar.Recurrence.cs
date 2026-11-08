using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar
{
    private static readonly DayOfWeek[] RepeatDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
    private string RepeatLabel(CalendarRepeatRule rule) => CalendarBuiltInRecurrence.Rules.ContainsKey(rule.Id) ? L[rule.Title].Value : $"{rule.Title} ({rule.Id})";

    private void ChangeRepeatRule(Microsoft.AspNetCore.Components.ChangeEventArgs args)
    {
        var id = args.Value?.ToString();
        if (string.IsNullOrEmpty(id))
        {
            _draft.SelectRepeatRule(null);
        }
        else if (SchedulingSnapshot.RepeatRules.TryGetValue(id, out var rule))
        {
            _draft.SelectRepeatRule(rule);
        }
    }

    private enum RepeatEnd
    {
        Never,
        OnDate,
        AfterCount
    }

    private sealed partial class EntryDraft
    {
        public string? RepeatRuleId { get; set; }
        public string? AdjustmentRuleId { get; set; }
        public string RepeatSelection => RepeatRuleId ?? CalendarBuiltInRecurrence.FromLegacy(RepeatPattern, RepeatInterval)?.Id ?? string.Empty;

        public void SelectRepeatRule(CalendarRepeatRule? rule)
        {
            RepeatRuleId = rule?.Id;
            RepeatPattern = rule?.Pattern ?? CalendarRepeatPattern.None;
            RepeatInterval = rule?.Interval ?? 1;
            RepeatWeekdays = CalendarWeekdays.None;
            RepeatWeekOfMonth = null;
            if (rule is null)
            {
                RepeatUntil = null;
                RepeatCount = null;
            }
        }

        public string AdjustmentSelection
        {
            get => AdjustmentRuleId ?? string.Empty;
            set => AdjustmentRuleId = string.IsNullOrEmpty(value) ? null : value;
        }

        public CalendarRepeatPattern RepeatPattern { get; set; }
        public int RepeatInterval { get; set; } = 1;
        public CalendarWeekdays RepeatWeekdays { get; set; }
        public int? RepeatWeekOfMonth { get; set; }
        public DateOnly? RepeatUntil { get; set; }
        public int? RepeatCount { get; set; }
        public RepeatEnd End
        {
            get => RepeatUntil is not null ? RepeatEnd.OnDate : RepeatCount is not null ? RepeatEnd.AfterCount : RepeatEnd.Never;
            set
            {
                RepeatUntil = value == RepeatEnd.OnDate ? RepeatUntil ?? Date : null;
                RepeatCount = value == RepeatEnd.AfterCount ? RepeatCount ?? 10 : null;
            }
        }

        public bool RepeatsOn(DayOfWeek day) => EffectiveWeekdays.HasFlag((CalendarWeekdays)(1 << (int)day));
        public void ToggleRepeatDay(DayOfWeek day) => RepeatWeekdays = EffectiveWeekdays ^ (CalendarWeekdays)(1 << (int)day);
        private CalendarWeekdays EffectiveWeekdays => RepeatWeekdays == CalendarWeekdays.None ? (CalendarWeekdays)(1 << (int)Date.DayOfWeek) : RepeatWeekdays;
        public SaveCalendarEventRequest ToEventRequest() => new()
        {
            Title = Title,
            Notes = Notes,
            Date = Date,
            StartTime = StartTime,
            EndTime = EndTime,
            Style = Style,
            RepeatRuleId = RepeatRuleId,
            AdjustmentRuleId = AdjustmentRuleId,
            RepeatPattern = RepeatPattern,
            RepeatInterval = RepeatInterval,
            RepeatWeekdays = RepeatWeekdays,
            RepeatWeekOfMonth = RepeatWeekOfMonth,
            RepeatUntil = RepeatUntil,
            RepeatCount = RepeatCount
        };

        public SaveCalendarTaskRequest ToTaskRequest() => new()
        {
            Title = Title,
            Notes = Notes,
            Date = Date,
            StartTime = StartTime,
            Style = Style,
            AdjustmentRuleId = AdjustmentRuleId
        };
    }
}
