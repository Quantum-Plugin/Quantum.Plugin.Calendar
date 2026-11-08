using Microsoft.AspNetCore.Components;
using Quantum.OfficialPlugins.Calendar;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using System.Reflection;
using CalendarPage = Quantum.OfficialPlugins.Calendar.UI.Pages.Calendar;

namespace Quantum.CalendarPlugin.Tests.UI.Pages;

public sealed class CalendarRecurrenceEditorTests
{
    [Theory]
    [InlineData("quantum.plugin.calendar/builtin-daily", CalendarRepeatPattern.Daily, 1)]
    [InlineData("quantum.plugin.calendar/builtin-biweekly", CalendarRepeatPattern.Weekly, 2)]
    [InlineData("example.company/quarterly", CalendarRepeatPattern.Monthly, 3)]
    [InlineData("example.school/term", CalendarRepeatPattern.None, 1)]
    public void BuiltInAndExternalSelectionsUseTheSameEditorPath(string id, CalendarRepeatPattern pattern, int interval)
    {
        var host = new SchedulingExtensionHost();
        using var service = new CalendarSchedulingExtensions(host);
        service.Start();
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint,
            SchedulingExtensionHost.Contribution("example.company", "quarterly", """{"title":"Quarterly","pattern":"Monthly","interval":3}"""),
            SchedulingExtensionHost.Contribution("example.school", "term", """{"title":"Term","dates":["2026-11-06"]}"""));
        var page = new CalendarPage(null!, scheduling: service);
        var change = typeof(CalendarPage).GetMethod("ChangeRepeatRule", BindingFlags.Instance | BindingFlags.NonPublic)!;
        change.Invoke(page, [new ChangeEventArgs { Value = id }]);
        var draft = typeof(CalendarPage).GetField("_draft", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;
        var request = (SaveCalendarEventRequest)draft.GetType().GetMethod("ToEventRequest")!.Invoke(draft, null)!;
        Assert.Equal(id, request.RepeatRuleId);
        Assert.Equal(pattern, request.RepeatPattern);
        Assert.Equal(interval, request.RepeatInterval);
        Assert.Equal(id, draft.GetType().GetProperty("RepeatSelection")!.GetValue(draft));
        change.Invoke(page, [new ChangeEventArgs { Value = string.Empty }]);
        request = (SaveCalendarEventRequest)draft.GetType().GetMethod("ToEventRequest")!.Invoke(draft, null)!;
        Assert.Null(request.RepeatRuleId);
        Assert.Equal(CalendarRepeatPattern.None, request.RepeatPattern);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditingAnOccurrencePreservesTheSeriesAnchorAndCompleteRule(bool contributed)
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
            RepeatRuleId = contributed ? "example.school/teaching-days" : null,
            AdjustmentRuleId = "example.company/shifts",
            RepeatPattern = contributed ? CalendarRepeatPattern.None : CalendarRepeatPattern.Weekly,
            RepeatInterval = contributed ? 1 : 2,
            RepeatWeekdays = contributed ? CalendarWeekdays.None : CalendarWeekdays.Monday | CalendarWeekdays.Friday,
            RepeatCount = 10
        };
        typeof(CalendarPage).GetMethod("BeginEdit", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, [occurrence]);
        var draft = typeof(CalendarPage).GetField("_draft", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)!;
        var request = (SaveCalendarEventRequest)draft.GetType().GetMethod("ToEventRequest")!.Invoke(draft, null)!;
        Assert.Equal(occurrence.SeriesStartDate, request.Date);
        Assert.Equal(occurrence.RepeatRuleId, request.RepeatRuleId);
        Assert.Equal(occurrence.AdjustmentRuleId, request.AdjustmentRuleId);
        Assert.Equal(occurrence.RepeatPattern, request.RepeatPattern);
        Assert.Equal(occurrence.RepeatInterval, request.RepeatInterval);
        Assert.Equal(occurrence.RepeatWeekdays, request.RepeatWeekdays);
        Assert.Equal(occurrence.RepeatCount, request.RepeatCount);
        Assert.Equal(occurrence.Id, typeof(CalendarPage).GetField("_editingId", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page));
    }
}
