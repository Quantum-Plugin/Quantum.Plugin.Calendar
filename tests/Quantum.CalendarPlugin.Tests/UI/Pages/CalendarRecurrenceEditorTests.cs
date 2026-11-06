using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using System.Reflection;
using CalendarPage = Quantum.OfficialPlugins.Calendar.UI.Pages.Calendar;

namespace Quantum.CalendarPlugin.Tests.UI.Pages;

public sealed class CalendarRecurrenceEditorTests
{
    [Fact]
    public void EditingAnOccurrencePreservesTheSeriesAnchorAndCompleteRule()
    {
        var page = new CalendarPage(null!);
        var occurrence = new CalendarEntryDetails
        {
            Id = Guid.NewGuid(),
            Title = "Planning",
            Notes = "Series",
            Kind = CalendarEntryKind.Event,
            Date = new(2026, 11, 13),
            SeriesStartDate = new(2026, 10, 30),
            StartTime = new(9, 0),
            EndTime = new(10, 0),
            Style = "blue",
            IsCompleted = false,
            RepeatPattern = CalendarRepeatPattern.Weekly,
            RepeatInterval = 2,
            RepeatWeekdays = CalendarWeekdays.Monday | CalendarWeekdays.Friday,
            RepeatCount = 10
        };
        typeof(CalendarPage).GetMethod("BeginEdit", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, [occurrence]);
        var draft = typeof(CalendarPage).GetField("_draft", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;
        var request = (SaveCalendarEventRequest)draft.GetType().GetMethod("ToEventRequest")!.Invoke(draft, null)!;
        Assert.Equal(occurrence.SeriesStartDate, request.Date);
        Assert.Equal(occurrence.RepeatPattern, request.RepeatPattern);
        Assert.Equal(occurrence.RepeatInterval, request.RepeatInterval);
        Assert.Equal(occurrence.RepeatWeekdays, request.RepeatWeekdays);
        Assert.Equal(occurrence.RepeatCount, request.RepeatCount);
        Assert.Equal(occurrence.Id, typeof(CalendarPage).GetField("_editingId", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page));
    }
}
