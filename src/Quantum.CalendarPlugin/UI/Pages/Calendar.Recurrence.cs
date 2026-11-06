using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar
{
    private static readonly DayOfWeek[] RepeatDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday];
    private string RepeatLabel(RepeatPreset preset) => L[preset switch
    {
        RepeatPreset.None => "Does not repeat",
        RepeatPreset.Daily => "Every day",
        RepeatPreset.Weekdays => "Every weekday (Monday-Friday)",
        RepeatPreset.Weekly => "Every week",
        RepeatPreset.Biweekly => "Every two weeks",
        RepeatPreset.Monthly => "Every month on the same date",
        RepeatPreset.MonthlyLastDay => "Every month on the last day",
        RepeatPreset.MonthlyWeekday => "Every month on an ordinal weekday",
        _ => "Every year"
    }];

    private enum RepeatPreset
    {
        None,
        Daily,
        Weekdays,
        Weekly,
        Biweekly,
        Monthly,
        MonthlyWeekday,
        MonthlyLastDay,
        Yearly
    }

    private enum RepeatEnd
    {
        Never,
        OnDate,
        AfterCount
    }

    private sealed partial class EntryDraft
    {
        public CalendarRepeatPattern RepeatPattern { get; set; }
        public int RepeatInterval { get; set; } = 1;
        public CalendarWeekdays RepeatWeekdays { get; set; }
        public int? RepeatWeekOfMonth { get; set; }
        public DateOnly? RepeatUntil { get; set; }
        public int? RepeatCount { get; set; }
        public RepeatPreset Preset
        {
            get => RepeatPattern == CalendarRepeatPattern.Weekly && RepeatInterval == 2 ? RepeatPreset.Biweekly : Enum.Parse<RepeatPreset>(RepeatPattern.ToString());
            set
            {
                RepeatPattern = value == RepeatPreset.Biweekly ? CalendarRepeatPattern.Weekly : Enum.Parse<CalendarRepeatPattern>(value.ToString());
                RepeatInterval = value == RepeatPreset.Biweekly ? 2 : 1;
                RepeatWeekdays = CalendarWeekdays.None;
                RepeatWeekOfMonth = null;
                if (value == RepeatPreset.None)
                {
                    RepeatUntil = null;
                    RepeatCount = null;
                }
            }
        }

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
            Style = Style
        };
    }
}
