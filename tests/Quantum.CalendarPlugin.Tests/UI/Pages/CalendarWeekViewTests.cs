using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.OfficialPlugins.Calendar.UI.Localization;
using System.Reflection;
using CalendarPage = Quantum.OfficialPlugins.Calendar.UI.Pages.Calendar;

namespace Quantum.CalendarPlugin.Tests.UI.Pages;

public sealed class CalendarWeekViewTests
{
    [Fact]
    public async Task NavigationQueriesWholeWeeksAcrossYearsAndRetainsTheSelectedWeekday()
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        await Invoke(page, "SetWeekViewAsync", true);
        AssertRange(client, new(2026, 12, 28), new(2027, 1, 3));
        await Invoke(page, "ChangePeriodAsync", 1);
        AssertRange(client, new(2027, 1, 4), new(2027, 1, 10));
        Assert.Equal(new DateOnly(2027, 1, 7), Get(page, "_selectedDate"));
        await Invoke(page, "SetWeekViewAsync", false);
        AssertRange(client, new(2026, 12, 28), new(2027, 2, 7));
        Assert.Equal(new DateOnly(2027, 1, 7), Get(page, "_selectedDate"));
    }

    [Fact]
    public async Task SelectingAnotherMonthWithinAWeekDoesNotDiscardOrReloadItsData()
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        await Invoke(page, "SetWeekViewAsync", true);
        var count = client.Queries.Count;
        await Invoke(page, "SelectDateAsync", new DateOnly(2027, 1, 1));
        Assert.Equal(count, client.Queries.Count);
        Assert.Equal(new DateOnly(2027, 1, 1), Get(page, "_visibleMonth"));
        await Invoke(page, "SetWeekViewAsync", false);
        AssertRange(client, new(2026, 12, 28), new(2027, 2, 7));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelingACardOrSlotEditorInTheNeighborMonthStillSwitchesToThatMonth(bool create)
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        await Invoke(page, "SetWeekViewAsync", true);
        if (create)
        {
            typeof(CalendarPage).GetMethod("BeginCreateWeekEvent", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, [new DateOnly(2027, 1, 1), 9]);
        }
        else
        {
            typeof(CalendarPage).GetMethod("BeginEditWeekEntry", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, [Entry(new(2027, 1, 1), CalendarEntryKind.Event)]);
        }

        typeof(CalendarPage).GetMethod("CancelEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, null);
        await Invoke(page, "SetWeekViewAsync", false);
        AssertRange(client, new(2026, 12, 28), new(2027, 2, 7));
        Assert.Equal(new DateOnly(2027, 1, 1), Get(page, "_selectedDate"));
    }

    [Fact]
    public async Task TodayAndEditingADistantTaskLoadTheMatchingWeek()
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        await Invoke(page, "SetWeekViewAsync", true);
        await Invoke(page, "ShowTodayAsync");
        var today = (DateOnly)Get(page, "_today")!;
        var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        AssertRange(client, monday, monday.AddDays(6));
        var task = Entry(new(2027, 2, 1), CalendarEntryKind.Task);
        await Invoke(page, "EditTaskAsync", task);
        AssertRange(client, new(2027, 2, 1), new(2027, 2, 7));
        Assert.True((bool)Get(page, "_isEditorOpen")!);
    }

    [Fact]
    public async Task EditingARepeatedOccurrenceKeepsTheVisibleWeekAfterSaving()
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        await Invoke(page, "SetWeekViewAsync", true);
        var occurrence = Entry(new(2026, 12, 31), CalendarEntryKind.Event) with { SeriesStartDate = new(2026, 1, 1), RepeatPattern = CalendarRepeatPattern.Weekly };
        typeof(CalendarPage).GetMethod("BeginEditWeekEntry", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, [occurrence]);
        await Invoke(page, "SaveAsync");
        AssertRange(client, new(2026, 12, 28), new(2027, 1, 3));
        Assert.Equal(new DateOnly(2026, 12, 31), Get(page, "_selectedDate"));
        Assert.Equal(new DateOnly(2026, 1, 1), client.LastUpdate!.Entry.Date);
    }

    [Fact]
    public void CreatingFromAnHourSlotUsesThatDateAndTime()
    {
        var (page, _) = CreatePage(new(2026, 12, 31));
        typeof(CalendarPage).GetMethod("BeginCreateWeekEvent", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, [new DateOnly(2027, 1, 2), 23]);
        var draft = Get(page, "_draft")!;
        var request = (SaveCalendarEventRequest)draft.GetType().GetMethod("ToEventRequest")!.Invoke(draft, null)!;
        Assert.Equal(new DateOnly(2027, 1, 2), request.Date);
        Assert.Equal(new TimeOnly(23, 0), request.StartTime);
        Assert.Equal(new TimeOnly(23, 59), request.EndTime);
        Assert.True((bool)Get(page, "_isEditorOpen")!);
    }

    [Fact]
    public async Task FailedQueriesClearItemsFromThePreviousPeriod()
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        Set(page, "_items", new[] { Entry(new(2026, 12, 1), CalendarEntryKind.Event) });
        client.FailQueries = true;
        await Invoke(page, "SetWeekViewAsync", true);
        Assert.Empty((IReadOnlyList<CalendarEntryDetails>)Get(page, "_items")!);
        Assert.Equal("Unable to load data.", Get(page, "_errorMessage"));
    }

    [Fact]
    public void WeekStatisticsIgnoreItemsFromThePreviousPeriodDuringNavigation()
    {
        var (page, _) = CreatePage(new(2026, 12, 31));
        Set(page, "_isWeekView", true);
        Set(page, "_items", Enumerable.Range(1, 20).Select(day => Entry(new(2026, 12, day), CalendarEntryKind.Event)).ToArray());
        Assert.Equal(0, typeof(CalendarPage).GetProperty("EventCount", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page));
        Assert.Equal(7, typeof(CalendarPage).GetProperty("FreeDayCount", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page));
    }

    [Fact]
    public async Task DateBoundariesDisableNavigationAndNeverOverflow()
    {
        var (page, client) = CreatePage(DateOnly.MaxValue);
        await Invoke(page, "SetWeekViewAsync", true);
        AssertRange(client, new(9999, 12, 27), DateOnly.MaxValue);
        var count = client.Queries.Count;
        await Invoke(page, "ChangePeriodAsync", 1);
        Assert.Equal(count, client.Queries.Count);
        await Invoke(page, "SetWeekViewAsync", false);
        AssertRange(client, new(9999, 11, 29), DateOnly.MaxValue);
        Assert.NotNull(typeof(CalendarPage).GetProperty("CalendarDays", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page));
        var (first, firstClient) = CreatePage(DateOnly.MinValue);
        await Invoke(first, "SetWeekViewAsync", true);
        AssertRange(firstClient, DateOnly.MinValue, new(1, 1, 7));
        count = firstClient.Queries.Count;
        await Invoke(first, "ChangePeriodAsync", -1);
        Assert.Equal(count, firstClient.Queries.Count);
        await Invoke(first, "SetWeekViewAsync", false);
        AssertRange(firstClient, DateOnly.MinValue, new(1, 2, 11));
    }

    [Fact]
    public async Task WeekMarkupRendersAllSevenDaysAndRepeatedOccurrencesWithAccessibleControls()
    {
        var html = await RenderViewAsync(true);
        Assert.Contains("Week schedule", html);
        Assert.Contains("aria-pressed=\"true\"", html);
        Assert.Contains("aria-pressed=\"false\"", html);
        Assert.Contains("2026-12-28", html);
        Assert.Contains("2027-01-03", html);
        Assert.Contains("Planning", html);
        Assert.Contains("Year-end review", html);
        Assert.Contains("New event on", html);
        Assert.Equal(168, System.Text.RegularExpressions.Regex.Matches(html, "class=\"week-slot\"").Count);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(html, "class=\"week-event ").Count);
        Assert.DoesNotContain("day-grid incoming-grid", html);
        Assert.DoesNotContain("Manage scheduling rules", html);
    }

    [Fact]
    public async Task MonthMarkupShowsItemsOnNeighboringMonthDates()
    {
        var html = await RenderViewAsync(false);
        Assert.Contains("day-grid incoming-grid", html);
        Assert.DoesNotContain("week-viewport", html);
        Assert.Contains("Calendar view", html);
        Assert.Contains("Previous month event", html);
        Assert.Contains("Next month event", html);
        Assert.Matches("""<footer\b[^>]*class="month-stats"[^>]*>\s*<div\b[^>]*>\s*<strong\b[^>]*>2</strong>""", html);
        Assert.DoesNotContain("Manage scheduling rules", html);
        Assert.DoesNotContain("Repeating event. Editing or deleting affects the entire series.", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdjustedOccurrencesOnTheSameDayRenderAsSeparateCards(bool week)
    {
        var html = await RenderViewAsync(week, collision: true);
        Assert.Equal(week ? 4 : 3, System.Text.RegularExpressions.Regex.Matches(html, week ? "class=\"week-event " : "class=\"agenda-entry ").Count);
        Assert.Contains("Adjusted from", html);
    }

    [Fact]
    public async Task DeletionRefreshesTasksEvenWhenOccurrenceExpansionFails()
    {
        var (page, client) = CreatePage(new(2026, 12, 31));
        var task = Entry(new(2026, 12, 31), CalendarEntryKind.Task);
        var unresolved = Entry(task.Date, CalendarEntryKind.Event) with { RepeatRuleId = "example.school/missing" };
        client.Items = [task, unresolved];
        Set(page, "_tasks", new[] { task });
        client.FailQueries = true;
        await Invoke(page, "DeleteAsync", task.Id, true);
        await Invoke(page, "DeleteAsync", task.Id, true);
        Assert.Empty((IReadOnlyList<CalendarEntryDetails>)Get(page, "_tasks")!);
        Assert.Empty((IReadOnlyList<CalendarEntryDetails>)Get(page, "_items")!);
        Assert.Equal("Unable to load data.", Get(page, "_errorMessage"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CalendarEntryKind.Event)]
    [InlineData(CalendarEntryKind.Task)]
    public async Task EditorKeepsSchedulingControlsInCollapsedAdvancedSettings(CalendarEntryKind? existingKind)
    {
        var html = await RenderViewAsync(false, editor: true, existingKind: existingKind);
        if (existingKind is null)
        {
            Assert.Contains("class=\"kind-switch\"", html);
        }
        else
        {
            Assert.DoesNotContain("class=\"kind-switch\"", html);
        }

        var drawer = Assert.Single(System.Text.RegularExpressions.Regex.Matches(html, """<details\b[^>]*\bclass="advanced-settings"[^>]*>[\s\S]*?</details>""")).Value;
        Assert.DoesNotMatch("""\sopen(?:\s|=|$)""", drawer[..drawer.IndexOf('>')]);
        Assert.Matches("""<summary\b[^>]*>Advanced settings</summary>""", drawer);
        Assert.Contains("Adjustment source", drawer);
        Assert.Contains("Keep original dates", drawer);
        Assert.Matches("""<option\b[^>]*value="example.company/shifts"[^>]*>Company shifts</option>""", drawer);
        Assert.DoesNotContain("Only this item follows the selected source.", html);
        Assert.DoesNotContain("The start date must match the repeat rule.", html);
        Assert.DoesNotContain("Repeating event. Editing or deleting affects the entire series.", html);
        if (existingKind == CalendarEntryKind.Task)
        {
            Assert.DoesNotContain("Repeat pattern", drawer);
            return;
        }

        foreach (var id in CalendarBuiltInRecurrence.Rules.Keys)
        {
            var option = $"""<option\b[^>]*\bvalue="{System.Text.RegularExpressions.Regex.Escape(id)}"[^>]*>""";
            Assert.Single(System.Text.RegularExpressions.Regex.Matches(drawer, option));
        }

        Assert.Contains("value=\"example.school/term\"", drawer);
        Assert.DoesNotContain("value=\"Daily\"", drawer);
    }

    internal static async Task<string> RenderViewAsync(bool week, bool collision = false, bool editor = false, CalendarEntryKind? existingKind = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        var client = DispatchProxy.Create<ICalendarRpcServiceClient, WeekClient>();
        var fake = (WeekClient)(object)client;
        fake.Items =
        [
            Entry(new(2026, 12, 31), CalendarEntryKind.Event) with { Title = "Planning", RepeatPattern = CalendarRepeatPattern.Weekly, SeriesStartDate = new(2026, 1, 1), EndTime = new(10, 30) },
            Entry(new(2026, 12, 31), CalendarEntryKind.Event) with { Title = "Year-end review", StartTime = new(9, 30), EndTime = new(11, 0), Style = "green" },
            Entry(new(2027, 1, 1), CalendarEntryKind.Event) with { Title = "Next month event", Style = "coral" },
            Entry(new(2027, 1, 1), CalendarEntryKind.Task) with { Title = "Send notes" },
            Entry(new(2026, 11, 30), CalendarEntryKind.Event) with { Title = "Previous month event" }
        ];
        if (collision)
        {
            fake.Items[0] = fake.Items[0] with { OriginalDate = new(2026, 12, 30) };
            fake.Items = [.. fake.Items, fake.Items[0] with { OriginalDate = new(2026, 12, 29) }];
        }

        if (editor)
        {
            var extensions = new SchedulingExtensionHost();
            var scheduling = new Quantum.OfficialPlugins.Calendar.CalendarSchedulingExtensions(extensions);
            scheduling.Start();
            extensions.Publish(Quantum.OfficialPlugins.Calendar.CalendarSchedulingExtensions.RecurrencePoint, SchedulingExtensionHost.Contribution("example.school", "term", """{"title":"Teaching term","dates":["2026-11-06"]}"""));
            extensions.Publish(Quantum.OfficialPlugins.Calendar.CalendarSchedulingExtensions.AdjustmentPoint, SchedulingExtensionHost.Contribution("example.company", "shifts", """{"title":"Company shifts","validFrom":"2026-12-01","validUntil":"2027-01-31","changes":[]}"""));
            services.AddSingleton(scheduling);
        }

        services.AddSingleton(client);
        services.AddSingleton<IJSRuntime, UnusedJavaScript>();
        var existingEntry = existingKind is { } kind ? fake.Items.First(item => item.Kind == kind) : null;
        services.AddSingleton<IComponentActivator>(provider => new WeekActivator(provider, week, editor, existingEntry));
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<CalendarPage>()).ToHtmlString());
    }

    private static (CalendarPage Page, WeekClient Client) CreatePage(DateOnly date)
    {
        var client = DispatchProxy.Create<ICalendarRpcServiceClient, WeekClient>();
        var page = new CalendarPage(client);
        using var factory = LoggerFactory.Create(_ => { });
        var localizers = new ResourceManagerStringLocalizerFactory(Microsoft.Extensions.Options.Options.Create(new LocalizationOptions()), factory);
        typeof(CalendarPage).GetProperty("L", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, new StringLocalizer<PluginStrings>(localizers));
        Set(page, "_selectedDate", date);
        Set(page, "_visibleMonth", new DateOnly(date.Year, date.Month, 1));
        return (page, (WeekClient)(object)client);
    }

    private static void AssertRange(WeekClient client, DateOnly start, DateOnly end)
    {
        Assert.Equal(start, client.Queries[^1].StartDate);
        Assert.Equal(end, client.Queries[^1].EndDate);
    }

    private static object? Get(CalendarPage page, string name) => typeof(CalendarPage).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);
    private static void Set(CalendarPage page, string name, object value) => typeof(CalendarPage).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);
    private static Task Invoke(CalendarPage page, string name, params object[] arguments) => (Task)typeof(CalendarPage).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, arguments)!;
    private static CalendarEntryDetails Entry(DateOnly date, CalendarEntryKind kind) => new() { Id = Guid.NewGuid(), Title = "Event", Notes = "", Kind = kind, Date = date, SeriesStartDate = date, StartTime = new(9, 0), EndTime = null, Style = "blue", IsCompleted = false };

    public class WeekClient : DispatchProxy
    {
        public List<ListCalendarEntriesRequest> Queries { get; } = [];
        public CalendarEntryDetails[] Items { get; set; } = [];
        public bool FailQueries { get; set; }
        public UpdateCalendarEventRequest? LastUpdate { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod!.Name)
            {
                case "ListEntriesAsync":
                    var request = (ListCalendarEntriesRequest)args![0]!;
                    Queries.Add(request);
                    return Task.FromResult<Result<CalendarEntryDetails[]>>(FailQueries ? Result.Fail("unavailable", "Unable to load data.") : Result.Success(Items.Where(item => item.Date >= request.StartDate && item.Date <= request.EndDate).ToArray()));
                case "GetEntryAsync":
                    var lookup = (CalendarEntryRequest)args![0]!;
                    return Task.FromResult(QuantumRequestResult<CalendarEntryLookup>.Answer(new CalendarEntryLookup { Entry = Items.SingleOrDefault(item => item.Id == lookup.Id) }));
                case "DeleteTaskAsync":
                    var deleted = (CalendarEntryRequest)args![0]!;
                    Items = Items.Where(item => item.Id != deleted.Id).ToArray();
                    return Task.FromResult(Result.Success());
                case "ListTasksAsync":
                    return Task.FromResult(Result.Success(Items.Where(item => item.Kind == CalendarEntryKind.Task).ToArray()));
                case "UpdateEventAsync":
                    LastUpdate = (UpdateCalendarEventRequest)args![0]!;
                    return Task.FromResult(Result.Success(Entry(LastUpdate.Entry.Date, CalendarEntryKind.Event) with { Id = LastUpdate.Id }));
                default:
                    throw new InvalidOperationException(targetMethod.Name);
            }
        }
    }

    private sealed class WeekActivator(IServiceProvider provider, bool week, bool editor, CalendarEntryDetails? existingEntry) : IComponentActivator
    {
        public IComponent CreateInstance(Type componentType)
        {
            var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
            if (component is CalendarPage page)
            {
                Set(page, "_selectedDate", new DateOnly(2026, 12, 31));
                Set(page, "_visibleMonth", new DateOnly(2026, 12, 1));
                Set(page, "_isWeekView", week);
                if (existingEntry is not null)
                {
                    typeof(CalendarPage).GetMethod("BeginEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, [existingEntry]);
                }
                else
                {
                    Set(page, "_isEditorOpen", editor);
                }
            }

            return component;
        }
    }

    private sealed class UnusedJavaScript : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }
}
