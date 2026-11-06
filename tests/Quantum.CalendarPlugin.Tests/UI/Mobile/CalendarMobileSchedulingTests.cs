using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.OfficialPlugins.Calendar.UI.Mobile;
using System.Reflection;

namespace Quantum.CalendarPlugin.Tests.UI.Mobile;

public sealed class CalendarMobileSchedulingTests
{
    [Fact]
    public async Task SameSeriesOccurrencesOnTheSameDayHaveUniqueMobileNodeIds()
    {
        var client = DispatchProxy.Create<ICalendarRpcServiceClient, MobileClient>();
        var fake = (MobileClient)(object)client;
        var entry = new CalendarEntryDetails { Id = Guid.NewGuid(), Title = "Adjusted meeting", Notes = "", Kind = CalendarEntryKind.Event, Date = new(2026, 11, 6), OriginalDate = new(2026, 11, 4), SeriesStartDate = new(2026, 11, 1), StartTime = new(9, 0), EndTime = new(10, 0), Style = "blue", IsCompleted = false };
        fake.Items = [entry, entry with { OriginalDate = new(2026, 11, 5) }];
        var pages = new MobilePages();
        await new CalendarMobilePages(client, pages, new FixedClock()).PublishAsync();
        var nodes = Flatten(pages.Page!.Root).ToArray();
        Assert.Equal(nodes.Length, nodes.Select(node => node.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, nodes.Count(node => node.Text == "Adjusted meeting"));
    }

    private static IEnumerable<MobileNode> Flatten(MobileNode node) => new[] { node }.Concat(node.Children.SelectMany(Flatten));

    public class MobileClient : DispatchProxy
    {
        public CalendarEntryDetails[] Items { get; set; } = [];
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod!.Name switch
        {
            "ListEntriesAsync" => Task.FromResult(Result.Success(Items)),
            "ListTasksAsync" => Task.FromResult(Result.Success(Array.Empty<CalendarEntryDetails>())),
            _ => throw new NotSupportedException()
        };
    }

    private sealed class MobilePages : IQuantumMobilePages
    {
        public MobilePage? Page { get; private set; }
        public void Publish(MobilePage page) => Page = page;
        public void Remove(string pageId) => Page = null;
    }

    private sealed class FixedClock : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
    }
}
