using NOF.Domain;
using Quantum.OfficialPlugins.Calendar;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.CalendarPlugin.Tests.Application;

public sealed class CalendarScheduleResolverTests
{
    private static readonly DateOnly Anchor = new(2026, 11, 6);

    [Theory]
    [InlineData("daily", 2026, 11, 6)]
    [InlineData("weekdays", 2026, 11, 6)]
    [InlineData("weekly", 2026, 11, 6)]
    [InlineData("biweekly", 2026, 11, 6)]
    [InlineData("monthly", 2026, 1, 31)]
    [InlineData("monthly-weekday", 2026, 1, 30)]
    [InlineData("monthly-last-day", 2026, 1, 31)]
    [InlineData("yearly", 2024, 2, 29)]
    public void BuiltInContributionsPreserveLegacyExpansion(string name, int year, int month, int day)
    {
        var snapshot = Snapshot();
        var id = $"quantum.plugin.calendar/builtin-{name}";
        var rule = snapshot.RepeatRules[id];
        var anchor = new DateOnly(year, month, day);
        var legacy = Entry() with { Date = anchor, RepeatPattern = rule.Pattern!.Value, RepeatInterval = rule.Interval, RepeatCount = 5 };
        var contributed = legacy with { RepeatRuleId = id };
        CalendarScheduleResolver.Validate(contributed, snapshot);
        var expected = Expand(legacy, snapshot, anchor, anchor.AddYears(20));
        var actual = Expand(contributed, snapshot, anchor, anchor.AddYears(20));
        Assert.Equal(5, actual.Length);
        Assert.Equal(expected.Select(item => (item.Id, item.Date, item.OriginalDate)), actual.Select(item => (item.Id, item.Date, item.OriginalDate)));
    }

    [Fact]
    public void ExternalPatternPresetsUseTheSameParametersAndRejectIncompatibleSavedOptions()
    {
        var host = new SchedulingExtensionHost();
        using var service = new CalendarSchedulingExtensions(host);
        service.Start();
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint, SchedulingExtensionHost.Contribution("example.company", "quarterly", """{"title":"Quarterly review","pattern":"Monthly","interval":3}"""));
        var item = Entry() with { RepeatRuleId = "example.company/quarterly", RepeatPattern = CalendarRepeatPattern.Monthly, RepeatInterval = 3, RepeatCount = 3 };
        CalendarScheduleResolver.Validate(item, service.Capture());
        Assert.Equal(new[] { Anchor, new DateOnly(2027, 2, 6), new(2027, 5, 6) }, Expand(item, service.Capture(), Anchor, new(2027, 12, 31)).Select(entry => entry.Date));
        Assert.Throws<DomainException>(() => CalendarScheduleResolver.Validate(item with { RepeatPattern = CalendarRepeatPattern.Daily }, service.Capture()));
        Assert.Throws<DomainException>(() => Expand(item with { RepeatPattern = CalendarRepeatPattern.Daily }, service.Capture(), Anchor, Anchor));
    }

    [Fact]
    public void AdjustmentRequiresAnExplicitChoiceAndCanMoveOrSkipAnOccurrence()
    {
        var snapshot = Snapshot();
        var ordinary = Entry();
        Assert.Equal(Anchor, Assert.Single(Expand(ordinary, snapshot, Anchor, Anchor)).Date);
        var optedIn = ordinary with { AdjustmentRuleId = "example.company/working-days" };
        Assert.Empty(Expand(optedIn, snapshot, Anchor, Anchor));
        var moved = Assert.Single(Expand(optedIn, snapshot, new(2026, 12, 1), new(2026, 12, 1)));
        Assert.Equal(Anchor, moved.OriginalDate);
        Assert.Equal(Anchor, moved.SeriesStartDate);
        Assert.Equal(ordinary.Id, moved.Id);
        Assert.Empty(Expand(optedIn with { Date = new(2026, 11, 7) }, snapshot, new(2026, 11, 1), new(2026, 12, 31)));
    }

    [Fact]
    public void CountAndUntilApplyToNominalDatesBeforeMovesAndCancellations()
    {
        var item = Entry() with { RepeatPattern = CalendarRepeatPattern.Daily, RepeatCount = 2, AdjustmentRuleId = "example.company/working-days" };
        var occurrences = Expand(item, Snapshot(), Anchor, new(2026, 12, 31));
        Assert.Equal(new DateOnly(2026, 12, 1), Assert.Single(occurrences).Date);
        Assert.Single(Expand(item with { RepeatCount = null, RepeatUntil = Anchor }, Snapshot(), new(2026, 12, 1), new(2026, 12, 1)));
    }

    [Fact]
    public void TeachingDatesAreSortedAnchoredAndBoundedWithoutBuiltInInstitutionRules()
    {
        var item = Entry() with { RepeatRuleId = "example.school/teaching-fridays", RepeatCount = 2 };
        var snapshot = Snapshot();
        CalendarScheduleResolver.Validate(item, snapshot);
        var occurrences = Expand(item, snapshot, new(2026, 11, 10), new(2026, 12, 31));
        Assert.Equal(new DateOnly(2026, 11, 20), Assert.Single(occurrences).Date);
        Assert.Empty(Expand(item with { RepeatCount = null, RepeatUntil = new(2026, 11, 19) }, snapshot, new(2026, 11, 10), new(2026, 12, 31)));
        Assert.Throws<DomainException>(() => CalendarScheduleResolver.Validate(item with { Date = new(2026, 11, 13) }, snapshot));
    }

    [Fact]
    public void UnavailableAndUncoveredSourcesFailExplicitlyInsteadOfGuessingDates()
    {
        var missing = Entry() with { RepeatRuleId = "example.school/missing" };
        Assert.Throws<DomainException>(() => Expand(missing, Snapshot(), Anchor, Anchor));
        Assert.Throws<DomainException>(() => Expand(Entry() with { AdjustmentRuleId = "example.company/missing" }, Snapshot(), Anchor, Anchor));
        Assert.Throws<DomainException>(() => Expand(Entry() with { Date = new(2027, 1, 1), AdjustmentRuleId = "example.company/working-days" }, Snapshot(), new(2027, 1, 1), new(2027, 1, 1)));
    }

    [Fact]
    public void TwoNominalDatesMovedToTheSameDayRemainDistinctAndAreNeverAdjustedTwice()
    {
        var changes = new Dictionary<DateOnly, DateOnly?> { [Anchor] = Anchor.AddDays(1), [Anchor.AddDays(1)] = Anchor.AddDays(1) };
        var snapshot = new CalendarScheduleSnapshot(new Dictionary<string, CalendarRepeatRule>(), new Dictionary<string, CalendarDateAdjustment> { ["example.company/collision"] = new("example.company/collision", "Collision", Anchor, Anchor.AddDays(1), changes) });
        var item = Entry() with { RepeatPattern = CalendarRepeatPattern.Daily, RepeatCount = 2, AdjustmentRuleId = "example.company/collision" };
        var occurrences = Expand(item, snapshot, Anchor.AddDays(1), Anchor.AddDays(1));
        Assert.Equal(2, occurrences.Length);
        Assert.Equal(2, occurrences.Select(occurrence => occurrence.OriginalDate).Distinct().Count());
        Assert.All(occurrences, occurrence => Assert.Equal(Anchor.AddDays(1), occurrence.Date));
    }

    [Fact]
    public void ExpansionHonorsCancellationAndExtremeDates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => CalendarScheduleResolver.Expand(Entry(), Snapshot(), Anchor, Anchor, cancellation.Token).ToArray());
        Assert.Equal(DateOnly.MaxValue, Assert.Single(Expand(Entry() with { Date = DateOnly.MaxValue }, CalendarScheduleSnapshot.Empty, DateOnly.MaxValue, DateOnly.MaxValue)).Date);
    }

    internal static CalendarScheduleSnapshot Snapshot()
    {
        var host = new SchedulingExtensionHost();
        using var extensions = new CalendarSchedulingExtensions(host);
        extensions.Start();
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint, SchedulingExtensionHost.Contribution("example.school", "teaching-fridays", """{"title":"Teaching Fridays","dates":["2026-12-04","2026-11-06","2026-11-20"]}"""));
        host.Publish(CalendarSchedulingExtensions.AdjustmentPoint, SchedulingExtensionHost.Contribution("example.company", "working-days", """{"title":"Company calendar","validFrom":"2026-01-01","validUntil":"2026-12-31","changes":[{"date":"2026-11-06","targetDate":"2026-12-01"},{"date":"2026-11-07","targetDate":null}]}"""));
        return extensions.Capture();
    }

    private static CalendarEntryDetails Entry() => new() { Id = Guid.NewGuid(), Title = "Meeting", Notes = "", Kind = CalendarEntryKind.Event, Date = Anchor, SeriesStartDate = Anchor, StartTime = new(9, 0), EndTime = new(10, 0), Style = "blue", IsCompleted = false };
    private static CalendarEntryDetails[] Expand(CalendarEntryDetails item, CalendarScheduleSnapshot snapshot, DateOnly start, DateOnly end) => CalendarScheduleResolver.Expand(item, snapshot, start, end).ToArray();
}
