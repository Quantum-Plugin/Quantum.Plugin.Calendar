using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.CalendarPlugin.Tests.Domain;

public sealed class CalendarRecurrenceTests
{
    [Fact]
    public void DailyIntervalAndCountAreAnchoredBeforeTheQueryWindow()
    {
        var entry = Event(new(2026, 11, 1));
        entry.SetRecurrence(CalendarRepeatPattern.Daily, interval: 2, count: 4);
        Assert.Equal(new[] { new DateOnly(2026, 11, 5), new(2026, 11, 7) }, Dates(entry, new(2026, 11, 4), new(2026, 12, 31)));
        Assert.Empty(Dates(entry, new(2027, 1, 1), new(2027, 1, 31)));
    }

    [Fact]
    public void WeekdaysSkipWeekendsAndIncludeTheEndDate()
    {
        var entry = Event(new(2026, 11, 6));
        entry.SetRecurrence(CalendarRepeatPattern.Weekdays, until: new(2026, 11, 10));
        Assert.Equal(new[] { new DateOnly(2026, 11, 6), new(2026, 11, 9), new(2026, 11, 10) }, Dates(entry, new(2026, 11, 1), new(2026, 12, 31)));
    }

    [Fact]
    public void BiweeklyUsesMondayBasedWeeksAndMultipleWeekdays()
    {
        var entry = Event(new(2026, 11, 6));
        entry.SetRecurrence(CalendarRepeatPattern.Weekly, interval: 2, weekdays: CalendarWeekdays.Monday | CalendarWeekdays.Friday);
        Assert.Equal(new[] { new DateOnly(2026, 11, 6), new(2026, 11, 16), new(2026, 11, 20), new(2026, 11, 30) }, Dates(entry, new(2026, 11, 1), new(2026, 11, 30)));
    }

    [Fact]
    public void WeeklyWithoutExplicitDaysUsesTheStartWeekday()
    {
        var entry = Event(new(2026, 11, 1));
        entry.SetRecurrence(CalendarRepeatPattern.Weekly);
        Assert.Equal(new[] { new DateOnly(2026, 11, 8), new(2026, 11, 15) }, Dates(entry, new(2026, 11, 2), new(2026, 11, 15)));
    }

    [Fact]
    public void MonthlySkipsInvalidDatesWithoutDriftingOrCountingThem()
    {
        var entry = Event(new(2026, 1, 31));
        entry.SetRecurrence(CalendarRepeatPattern.Monthly, count: 3);
        Assert.Equal(new[] { new DateOnly(2026, 1, 31), new(2026, 3, 31), new(2026, 5, 31) }, Dates(entry, new(2026, 1, 1), new(2027, 1, 1)));
    }

    [Fact]
    public void LastDayAndQuarterlyPatternsHandleShortMonths()
    {
        var entry = Event(new(2028, 1, 31));
        entry.SetRecurrence(CalendarRepeatPattern.MonthlyLastDay);
        Assert.Equal(new[] { new DateOnly(2028, 1, 31), new(2028, 2, 29), new(2028, 3, 31) }, Dates(entry, new(2028, 1, 1), new(2028, 3, 31)));
        entry.SetRecurrence(CalendarRepeatPattern.MonthlyLastDay, interval: 3);
        Assert.Equal(new[] { new DateOnly(2028, 1, 31), new(2028, 4, 30), new(2028, 7, 31), new(2028, 10, 31) }, Dates(entry, new(2028, 1, 1), new(2028, 12, 31)));
    }

    [Fact]
    public void OrdinalAndLastWeekdaysHaveDifferentSemantics()
    {
        var entry = Event(new(2026, 1, 30));
        entry.SetRecurrence(CalendarRepeatPattern.MonthlyWeekday);
        Assert.Equal(new[] { new DateOnly(2026, 1, 30), new(2026, 5, 29) }, Dates(entry, new(2026, 1, 1), new(2026, 5, 31)));
        entry.SetRecurrence(CalendarRepeatPattern.MonthlyWeekday, weekOfMonth: -1, count: 3);
        Assert.Equal(new[] { new DateOnly(2026, 1, 30), new(2026, 2, 27), new(2026, 3, 27) }, Dates(entry, new(2026, 1, 1), new(2026, 5, 31)));
    }

    [Fact]
    public void YearlyLeapDaySkipsNonLeapYears()
    {
        var entry = Event(new(2024, 2, 29));
        entry.SetRecurrence(CalendarRepeatPattern.Yearly, count: 3);
        Assert.Equal(new[] { new DateOnly(2024, 2, 29), new(2028, 2, 29), new(2032, 2, 29) }, Dates(entry, new(2024, 1, 1), new(2036, 1, 1)));
    }

    [Fact]
    public void NoRepeatReturnsOnlyTheOriginalDateAndMaximumDateDoesNotOverflow()
    {
        var entry = Event(DateOnly.MaxValue);
        Assert.Equal(new[] { DateOnly.MaxValue }, Dates(entry, DateOnly.MaxValue, DateOnly.MaxValue));
        entry.SetRecurrence(CalendarRepeatPattern.Daily);
        Assert.Equal(new[] { DateOnly.MaxValue }, Dates(entry, DateOnly.MaxValue, DateOnly.MaxValue));
        Assert.Empty(Dates(entry, new(2026, 1, 1), new(2026, 1, 31)));
    }

    [Theory]
    [InlineData(0, 0, 0, null, null)]
    [InlineData(1, 1000, 0, null, null)]
    [InlineData(3, 1, 128, null, null)]
    [InlineData(1, 1, 2, null, null)]
    [InlineData(1, 1, 0, 2, null)]
    [InlineData(1, 1, 0, null, 0)]
    [InlineData(1, 1, 0, null, 10001)]
    [InlineData(0, 1, 0, null, 1)]
    [InlineData(2, 2, 0, null, null)]
    [InlineData(99, 1, 0, null, null)]
    public void InvalidRulesAreRejectedWithoutChangingTheExistingRule(int pattern, int interval, int days, int? week, int? count)
    {
        var entry = Event(new(2026, 11, 6));
        entry.SetRecurrence(CalendarRepeatPattern.Daily, count: 5);
        Assert.Throws<DomainException>(() => entry.SetRecurrence((CalendarRepeatPattern)pattern, interval, (CalendarWeekdays)days, week, count: count));
        Assert.Equal(CalendarRepeatPattern.Daily, entry.RepeatPattern);
        Assert.Equal(5, entry.RepeatCount);
    }

    [Fact]
    public void StartAndEndConditionsMustBeConsistent()
    {
        var entry = Event(new(2026, 11, 6));
        Assert.Throws<DomainException>(() => entry.SetRecurrence(CalendarRepeatPattern.Daily, until: new(2026, 11, 5)));
        Assert.Throws<DomainException>(() => entry.SetRecurrence(CalendarRepeatPattern.Daily, until: new(2026, 12, 1), count: 2));
        Assert.Throws<DomainException>(() => entry.SetRecurrence(CalendarRepeatPattern.Weekly, weekdays: CalendarWeekdays.Monday));
        Assert.Throws<DomainException>(() => entry.SetRecurrence(CalendarRepeatPattern.MonthlyWeekday, weekOfMonth: -1));
        Assert.Throws<DomainException>(() => entry.SetRecurrence(CalendarRepeatPattern.MonthlyLastDay));
        var weekend = Event(new(2026, 11, 7));
        Assert.Throws<DomainException>(() => weekend.SetRecurrence(CalendarRepeatPattern.Weekdays));
    }

    [Fact]
    public void TasksCannotRepeatAndEventsCannotBeCompleted()
    {
        var task = CalendarEntry.Create("Task", "", CalendarEntryKind.Task, new(2026, 11, 6), new(9, 0), null, "blue", TimeProvider.System);
        Assert.Throws<DomainException>(() => task.SetRecurrence(CalendarRepeatPattern.Daily));
        Assert.Throws<DomainException>(() => Event(new(2026, 11, 6)).SetCompleted(true, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ExpansionPropagatesCancellation()
    {
        var entry = Event(new(2026, 11, 6));
        entry.SetRecurrence(CalendarRepeatPattern.Daily);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => CalendarRecurrence.Expand(entry, entry.Date, entry.Date.AddDays(5), cancelled.Token).ToArray());
    }

    private static CalendarEntry Event(DateOnly date) => CalendarEntry.Create("Event", "", CalendarEntryKind.Event, date, new(9, 0), new(10, 0), "blue", TimeProvider.System);
    private static DateOnly[] Dates(CalendarEntry entry, DateOnly start, DateOnly end) => CalendarRecurrence.Expand(entry, start, end).ToArray();
}
