using Quantum.OfficialPlugins.Calendar;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.CalendarPlugin.Tests.Application;

public sealed class CalendarSchedulingExtensionsTests
{
    [Fact]
    public void LifecycleReplacesSnapshotsIsolatesBadContributionsAndRejectsRetiredCallbacks()
    {
        var host = new SchedulingExtensionHost();
        using var service = new CalendarSchedulingExtensions(host);
        service.Start();
        service.Start();
        Assert.Equal(new[] { "recurrence-rules", "date-adjustments" }, host.Definitions);
        Assert.Equal(2, host.ObservationCount);
        Assert.Equal(8, host.RegistrationCount);
        Assert.Equal(CalendarBuiltInRecurrence.Rules.Keys.Order(), service.Capture().RepeatRules.Keys.Order());
        var valid = SchedulingExtensionHost.Contribution("example.school", "term", """{"title":"Term","dates":["2026-11-06"]}""");
        var invalid = new[] { "null", "[]", "{}", """{"title":"","dates":["2026-11-06"]}""", """{"title":"Bad","dates":["2026-11-06","2026-11-06"]}""", """{"title":"Bad","dates":["not-a-date"]}""" };
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint, [valid, .. invalid.Select((json, index) => SchedulingExtensionHost.Contribution("example.invalid", $"rule-{index}", json))]);
        var captured = service.Capture();
        Assert.Equal("example.school/term", Assert.Single(External(captured)).Key);
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint);
        Assert.Empty(External(service.Capture()));
        Assert.Single(External(captured));
        service.Dispose();
        Assert.Empty(host.Definitions);
        Assert.Equal(0, host.RegistrationCount);
        Assert.Equal(0, host.ObservationCount);
        service.Start();
        foreach (var observer in host.RetiredObservers)
        {
            observer(new(CalendarSchedulingExtensions.RecurrencePoint, 1, 100, true, [valid]));
        }

        Assert.Empty(External(service.Capture()));
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint, valid);
        Assert.Single(External(service.Capture()));
    }

    [Fact]
    public void PatternContributionsShareTheCatalogAndBadDescriptorsAreIsolated()
    {
        var host = new SchedulingExtensionHost();
        using var service = new CalendarSchedulingExtensions(host);
        service.Start();
        var valid = SchedulingExtensionHost.Contribution("example.company", "quarterly", """{"title":"Quarterly review","pattern":"Monthly","interval":3}""");
        var invalid = new[]
        {
            """{"title":"Bad","pattern":"None"}""",
            """{"title":"Bad","pattern":"1"}""",
            """{"title":"Bad","pattern":"Unknown"}""",
            """{"title":"Bad","pattern":"Daily","interval":0}""",
            """{"title":"Bad","pattern":"Weekdays","interval":2}""",
            """{"title":"Bad","pattern":"Daily","dates":["2026-11-06"]}""",
            """{"title":"Bad","dates":["2026-11-06"],"interval":2}"""
        };
        host.Publish(CalendarSchedulingExtensions.RecurrencePoint, [valid, .. invalid.Select((json, index) => SchedulingExtensionHost.Contribution("example.invalid", $"rule-{index}", json))]);
        var rule = Assert.Single(External(service.Capture())).Value;
        Assert.Equal(CalendarRepeatPattern.Monthly, rule.Pattern);
        Assert.Equal(3, rule.Interval);
        Assert.Equal(9, service.Capture().RepeatRules.Count);
        Assert.All(CalendarBuiltInRecurrence.Rules, expected => Assert.Equal(expected.Value, service.Capture().RepeatRules[expected.Key]));
    }

    private static IEnumerable<KeyValuePair<string, CalendarRepeatRule>> External(CalendarScheduleSnapshot snapshot) => snapshot.RepeatRules.Where(rule => !CalendarBuiltInRecurrence.Rules.ContainsKey(rule.Key));

    [Theory]
    [InlineData("{\"title\":\"Bad\",\"validFrom\":\"2026-12-31\",\"validUntil\":\"2026-01-01\",\"changes\":[]}")]
    [InlineData("{\"title\":\"Bad\",\"validFrom\":\"2026-01-01\",\"validUntil\":\"2026-12-31\",\"changes\":[{\"date\":\"2026-11-06\",\"targetDate\":\"2027-01-01\"}]}")]
    [InlineData("{\"title\":\"Bad\",\"validFrom\":\"2026-01-01\",\"validUntil\":\"2026-12-31\",\"changes\":[{\"date\":\"2026-11-06\",\"targetDate\":null},{\"date\":\"2026-11-06\",\"targetDate\":null}]}")]
    public void InvalidAdjustmentsAreExcluded(string json)
    {
        var host = new SchedulingExtensionHost();
        using var service = new CalendarSchedulingExtensions(host);
        service.Start();
        host.Publish(CalendarSchedulingExtensions.AdjustmentPoint, SchedulingExtensionHost.Contribution("example.company", "bad", json));
        Assert.Empty(service.Capture().Adjustments);
    }
}
