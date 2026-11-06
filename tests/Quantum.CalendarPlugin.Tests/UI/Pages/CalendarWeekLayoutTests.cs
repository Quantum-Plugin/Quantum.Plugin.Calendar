using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.OfficialPlugins.Calendar.UI.Pages;

namespace Quantum.CalendarPlugin.Tests.UI.Pages;

public sealed class CalendarWeekLayoutTests
{
    [Fact]
    public void OverlappingEventsUseSeparateColumnsAndReuseFreedColumns()
    {
        var first = Event(9, 0, 10, 30);
        var second = Event(9, 30, 10, 0);
        var third = Event(10, 0, 11, 0);
        var separate = Event(11, 0, 12, 0);
        var layouts = CalendarWeekLayout.Arrange([third, separate, second, first]);
        Assert.Equal(new[] { first.Id, second.Id, third.Id, separate.Id }, layouts.Select(layout => layout.Entry.Id));
        Assert.Equal(new[] { 0, 1, 1, 0 }, layouts.Select(layout => layout.Column));
        Assert.Equal(new[] { 2, 2, 2, 1 }, layouts.Select(layout => layout.ColumnCount));
    }

    [Fact]
    public void OverlapChainsAndEqualStartTimesNeverShareAVisualColumn()
    {
        var layouts = CalendarWeekLayout.Arrange([Event(9, 0, 10, 0), Event(9, 0, 11, 0), Event(9, 0, 12, 0), Event(10, 0, 13, 0)]);
        Assert.All(layouts, layout => Assert.Equal(3, layout.ColumnCount));
        foreach (var first in layouts)
        {
            foreach (var second in layouts.Where(second => second.Entry.Id != first.Entry.Id && second.StartMinute < first.EndMinute && second.EndMinute > first.StartMinute))
            {
                Assert.NotEqual(first.Column, second.Column);
            }
        }
    }

    [Fact]
    public void ShortAndLateEventsRemainVisibleAndTasksStayOffTheTimeline()
    {
        var shortEvent = Event(9, 0, 9, 5);
        var nearEvent = Event(9, 10, 9, 15);
        var late = Event(23, 59, null, null);
        var task = shortEvent with { Id = Guid.NewGuid(), Kind = CalendarEntryKind.Task };
        var layouts = CalendarWeekLayout.Arrange([task, shortEvent, nearEvent, late]);
        Assert.Equal(3, layouts.Count);
        Assert.Equal(2, layouts[0].ColumnCount);
        Assert.Equal(2, layouts[1].ColumnCount);
        Assert.Equal(1410, layouts[2].StartMinute);
        Assert.Equal(1440, layouts[2].EndMinute);
        Assert.All(layouts, layout => Assert.True(layout.EndMinute - layout.StartMinute >= 30));
    }

    [Fact]
    public void EventsWithoutEndTimesHaveThirtyMinuteFootprints()
    {
        var layout = Assert.Single(CalendarWeekLayout.Arrange([Event(9, 0, null, null)]));
        Assert.Equal(540, layout.StartMinute);
        Assert.Equal(570, layout.EndMinute);
    }

    private static CalendarEntryDetails Event(int hour, int minute, int? endHour, int? endMinute) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Event",
        Notes = "",
        Kind = CalendarEntryKind.Event,
        Date = new(2026, 12, 31),
        StartTime = new(hour, minute),
        EndTime = endHour is { } end ? new TimeOnly(end, endMinute!.Value) : null,
        Style = "blue",
        IsCompleted = false
    };
}
