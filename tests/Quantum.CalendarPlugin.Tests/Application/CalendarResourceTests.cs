using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NOF.Contract;
using NOF.Hosting;
using NOF.Infrastructure;
using NOF.Infrastructure.EntityFrameworkCore;
using Quantum.OfficialPlugins.Calendar;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using System.Reflection;
using System.Text.Json;

namespace Quantum.CalendarPlugin.Tests.Application;

public sealed class CalendarResourceTests
{
    [Fact]
    public void ManifestMatchesResourceCapabilitiesAndRemovesLegacyEntrypoints()
    {
        var requests = typeof(ICalendarRpcService).GetMethods().Select(method => method.GetCustomAttribute<RequestCapabilityAttribute>()!.Name).ToArray();
        var expected = new[] { "calendar.entry.get", "calendar.entry.list", "calendar.mobile-page.refresh", "calendar.task.set-completed" }
            .Concat(new[] { "event", "task" }.SelectMany(resource => new[] { "create", "delete", "get", "list", "update" }.Select(action => $"calendar.{resource}.{action}"))).Order().ToArray();
        Assert.Equal(expected, requests.Order());
        Assert.Equal(requests.Length, requests.Distinct().Count());
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "plugin-manifests/calendar.json")));
        var capabilities = manifest.RootElement.GetProperty("capabilities");
        var notifications = capabilities.GetProperty("notifications").EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Equal(new[] { "event", "task" }.SelectMany(resource => new[] { "created", "deleted", "updated" }.Select(action => $"calendar.{resource}.{action}")).Order(), notifications.Order());
        Assert.Equal(requests.Concat(notifications).Order(), capabilities.GetProperty("provides").EnumerateArray().Select(value => value.GetString()).Order());
        Assert.Null(typeof(SaveCalendarTaskRequest).GetProperty("Kind"));
        Assert.Null(typeof(SaveCalendarTaskRequest).GetProperty("RepeatPattern"));
        Assert.Null(typeof(SaveCalendarEventRequest).GetProperty("Kind"));
    }

    [Fact]
    public async Task ResourceOperationsPersistExpandAndProtectResourceOwnershipAcrossScopes()
    {
        await using var connection = new SqliteConnection($"Data Source=calendar-resources-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await Migrate(connection);
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.Clear();
        builder.Services.AddSingleton(builder.Environment);
        builder.Services.AddNOFApplication();
        builder.AddNOFEntityFrameworkCore();
        builder.UseDbContext<PluginTestDb>().WithTenantMode(TenantMode.SharedDatabase).WithConnectionString(connection.ConnectionString).WithOptions((options, cs) => options.UseSqlite(cs).EnableServiceProviderCaching(false));
        CalendarPluginInitializer.Initialize(builder.Services);
        builder.Services.AddLogging();
        builder.Services.AddSingleton(TimeProvider.System);
        await using var host = new LocalRpcHarness(builder.Services, typeof(ICalendarRpcService).Assembly);
        var request = new SaveCalendarEventRequest { Title = "Team sync", Notes = "Series", Date = new(2026, 10, 30), StartTime = new(9, 0), EndTime = new(10, 0), Style = "blue", RepeatPattern = CalendarRepeatPattern.Weekly, RepeatCount = 4 };
        Guid id = default;
        Guid taskId = default;
        await host.UseClient<ICalendarRpcServiceClient>(async client =>
        {
            var created = await client.CreateEventAsync(request, Context.Empty);
            Assert.True(created.IsSuccess, created.Message);
            id = created.Value!.Id;
            var task = await client.CreateTaskAsync(new() { Title = "Task", Notes = "", Date = new(2026, 11, 6), StartTime = new(12, 0), Style = "green" }, Context.Empty);
            Assert.True(task.IsSuccess, task.Message);
            taskId = task.Value!.Id;
        });
        await host.UseClient<ICalendarRpcServiceClient>(async client =>
        {
            var range = new ListCalendarEventsRequest { StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 30) };
            var occurrences = await client.ListEventsAsync(range, Context.Empty);
            Assert.True(occurrences.IsSuccess, occurrences.Message);
            Assert.Equal(new[] { new DateOnly(2026, 11, 6), new(2026, 11, 13), new(2026, 11, 20) }, occurrences.Value!.Select(item => item.Date));
            Assert.All(occurrences.Value!, item =>
            {
                Assert.Equal(id, item.Id);
                Assert.Equal(request.Date, item.SeriesStartDate);
                Assert.Equal(4, item.RepeatCount);
            });
            Assert.Equal(4, (await client.ListEntriesAsync(new() { StartDate = range.StartDate, EndDate = range.EndDate }, Context.Empty)).Value!.Length);
            Assert.Null((await client.GetTaskAsync(new() { Id = id }, Context.Empty)).Value!.Entry);
            Assert.Null((await client.GetEventAsync(new() { Id = taskId }, Context.Empty)).Value!.Entry);
            Assert.False((await client.DeleteTaskAsync(new() { Id = id }, Context.Empty)).IsSuccess);
            Assert.False((await client.DeleteEventAsync(new() { Id = taskId }, Context.Empty)).IsSuccess);
            Assert.False((await client.SetTaskCompletedAsync(new() { Id = id, IsCompleted = true }, Context.Empty)).IsSuccess);
            Assert.False((await client.UpdateEventAsync(new() { Id = taskId, Entry = request }, Context.Empty)).IsSuccess);
            Assert.False((await client.UpdateEventAsync(new() { Id = id, Entry = request with { RepeatInterval = 0, Title = "Invalid" } }, Context.Empty)).IsSuccess);
            Assert.False((await client.UpdateEventAsync(new() { Id = id, Entry = request with { Title = " " } }, Context.Empty)).IsSuccess);
            var unchanged = (await client.GetEventAsync(new() { Id = id }, Context.Empty)).Value!.Entry!;
            Assert.Equal("Team sync", unchanged.Title);
            Assert.Equal(4, unchanged.RepeatCount);
            Assert.Equal(request.Date, unchanged.Date);
            Assert.False((await client.ListEventsAsync(range with { EndDate = new(2040, 1, 1) }, Context.Empty)).IsSuccess);
            Assert.False((await client.ListEventsAsync(range with { EndDate = new(2026, 10, 1) }, Context.Empty)).IsSuccess);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            Assert.False((await client.ListEventsAsync(range, Context.Empty, cancelled.Token)).IsSuccess);
            Assert.True((await client.SetTaskCompletedAsync(new() { Id = taskId, IsCompleted = true }, Context.Empty)).IsSuccess);
            Assert.Empty((await client.ListTasksAsync(new() { IncludeCompleted = false }, Context.Empty)).Value!);
            var update = await client.UpdateEventAsync(new() { Id = id, Entry = request with { RepeatPattern = CalendarRepeatPattern.None, RepeatCount = null } }, Context.Empty);
            Assert.True(update.IsSuccess, update.Message);
            Assert.Empty((await client.ListEventsAsync(range, Context.Empty)).Value!);
            Assert.True((await client.DeleteEventAsync(new() { Id = id }, Context.Empty)).IsSuccess);
            Assert.Null((await client.GetEntryAsync(new() { Id = id }, Context.Empty)).Value!.Entry);
            Assert.True((await client.DeleteTaskAsync(new() { Id = taskId }, Context.Empty)).IsSuccess);
        });
    }

    [Fact]
    public async Task UpgradePreservesExistingRowsAsNonRepeatingEntries()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await Migrate(connection, 2);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO OfficialCalendarEntries (Id, Title, Notes, Kind, Date, StartTime, Style, CreatedAt, UpdatedAt, TenantId) VALUES ($id, 'Existing', 'Keep', 0, '2026-11-06', '09:00:00', 'blue', '2026-11-06T00:00:00Z', '2026-11-06T00:00:00Z', 'tenant')";
        var id = Guid.NewGuid().ToString();
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
        command.CommandText = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "calendar-migrations/003_event_recurrence.sql"));
        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT Id, Title, Notes, RepeatPattern, RepeatInterval, RepeatWeekdays, RepeatWeekOfMonth, RepeatUntil, RepeatCount FROM OfficialCalendarEntries";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(id, reader.GetString(0));
        Assert.Equal("Existing", reader.GetString(1));
        Assert.Equal("Keep", reader.GetString(2));
        Assert.Equal(0, reader.GetInt32(3));
        Assert.Equal(1, reader.GetInt32(4));
        Assert.Equal(0, reader.GetInt32(5));
        Assert.True(reader.IsDBNull(6) && reader.IsDBNull(7) && reader.IsDBNull(8));
    }

    [Fact]
    public async Task SchedulingSelectionsPersistThroughGeneratedClientsAndMovedDatesCrossQueryBoundaries()
    {
        await using var connection = new SqliteConnection($"Data Source=scheduling-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await connection.OpenAsync();
        await Migrate(connection);
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.Clear();
        builder.Services.AddSingleton(builder.Environment);
        builder.Services.AddNOFApplication();
        builder.AddNOFEntityFrameworkCore();
        builder.UseDbContext<PluginTestDb>().WithTenantMode(TenantMode.SharedDatabase).WithConnectionString(connection.ConnectionString).WithOptions((options, cs) => options.UseSqlite(cs).EnableServiceProviderCaching(false));
        builder.Services.AddLogging();
        builder.Services.AddSingleton(TimeProvider.System);
        var extensions = new SchedulingExtensionHost();
        builder.Services.AddSingleton<IQuantumExtensions>(extensions);
        CalendarPluginInitializer.Initialize(builder.Services);
        await using var host = new LocalRpcHarness(builder.Services, typeof(ICalendarRpcService).Assembly);
        await host.UseClient<CalendarSchedulingExtensions>(service =>
        {
            service.Start();
            return Task.CompletedTask;
        });
        extensions.Publish(CalendarSchedulingExtensions.RecurrencePoint, SchedulingExtensionHost.Contribution("example.school", "teaching-days", """{"title":"Teaching days","dates":["2026-11-06","2026-11-20"]}"""));
        extensions.Publish(CalendarSchedulingExtensions.AdjustmentPoint, SchedulingExtensionHost.Contribution("example.company", "shifts", """{"title":"Shifts","validFrom":"2026-01-01","validUntil":"2026-12-31","changes":[{"date":"2026-11-06","targetDate":"2026-12-01"}]}"""));
        var request = new SaveCalendarEventRequest { Title = "Seminar", Notes = "", Date = new(2026, 11, 6), StartTime = new(9, 0), Style = "blue", RepeatRuleId = "example.school/teaching-days", RepeatCount = 1, AdjustmentRuleId = "example.company/shifts" };
        Guid id = default;
        Guid taskId = default;
        await host.UseClient<ICalendarRpcServiceClient>(async client =>
        {
            var created = await client.CreateEventAsync(request, Context.Empty);
            Assert.True(created.IsSuccess, created.Message);
            id = created.Value!.Id;
            var task = await client.CreateTaskAsync(new() { Title = "Deadline", Notes = "", Date = request.Date, StartTime = request.StartTime, Style = "green", AdjustmentRuleId = request.AdjustmentRuleId }, Context.Empty);
            Assert.True(task.IsSuccess, task.Message);
            taskId = task.Value!.Id;
        });
        await host.UseClient<ICalendarRpcServiceClient>(async client =>
        {
            var saved = (await client.GetEventAsync(new() { Id = id }, Context.Empty)).Value!.Entry!;
            Assert.Equal(request.RepeatRuleId, saved.RepeatRuleId);
            Assert.Equal(request.AdjustmentRuleId, saved.AdjustmentRuleId);
            Assert.Equal(request.Date, saved.Date);
            var result = await client.ListEventsAsync(new() { StartDate = new(2026, 12, 1), EndDate = new(2026, 12, 1) }, Context.Empty);
            Assert.True(result.IsSuccess, result.Message);
            var moved = Assert.Single(result.Value!);
            Assert.Equal(new DateOnly(2026, 12, 1), moved.Date);
            Assert.Equal(request.Date, moved.OriginalDate);
            Assert.Equal(id, moved.Id);
            var tasks = await client.ListTasksAsync(new(), Context.Empty);
            Assert.Equal(taskId, Assert.Single(tasks.Value!).Id);
            Assert.Equal(moved.Date, tasks.Value![0].Date);
            var invalid = await client.UpdateEventAsync(new() { Id = id, Entry = request with { Title = "Should not persist", RepeatRuleId = "example.school/missing" } }, Context.Empty);
            Assert.False(invalid.IsSuccess);
            Assert.Equal("Seminar", (await client.GetEventAsync(new() { Id = id }, Context.Empty)).Value!.Entry!.Title);
            Assert.False((await client.CreateEventAsync(request with { RepeatPattern = CalendarRepeatPattern.Daily }, Context.Empty)).IsSuccess);
            extensions.Publish(CalendarSchedulingExtensions.RecurrencePoint);
            Assert.False((await client.ListEventsAsync(new() { StartDate = request.Date, EndDate = new(2026, 12, 31) }, Context.Empty)).IsSuccess);
            var stored = await client.ListEntriesAsync(new() { StartDate = DateOnly.MinValue, EndDate = DateOnly.MaxValue, SeriesPage = 0 }, Context.Empty);
            Assert.True(stored.IsSuccess, stored.Message);
            Assert.Equal(2, stored.Value!.Length);
            Assert.Equal(request.Date, Assert.Single(stored.Value!, item => item.Id == id).Date);
            Assert.Empty((await client.ListEntriesAsync(new() { StartDate = DateOnly.MinValue, EndDate = DateOnly.MaxValue, SeriesPage = 1 }, Context.Empty)).Value!);
            Assert.False((await client.ListEntriesAsync(new() { StartDate = DateOnly.MinValue, EndDate = DateOnly.MaxValue, SeriesPage = -1 }, Context.Empty)).IsSuccess);
            // Direct retrieval remains available so a caller can repair the unavailable selection.
            Assert.Equal(request.RepeatRuleId, (await client.GetEventAsync(new() { Id = id }, Context.Empty)).Value!.Entry!.RepeatRuleId);
            Assert.True((await client.UpdateEventAsync(new() { Id = id, Entry = request with { RepeatRuleId = null, RepeatCount = null, AdjustmentRuleId = null } }, Context.Empty)).IsSuccess);
            var builtIn = request with { RepeatRuleId = "quantum.plugin.calendar/builtin-weekly", RepeatPattern = CalendarRepeatPattern.Weekly, RepeatCount = 2, AdjustmentRuleId = null };
            Assert.True((await client.UpdateEventAsync(new() { Id = id, Entry = builtIn }, Context.Empty)).IsSuccess);
            var savedBuiltIn = (await client.GetEventAsync(new() { Id = id }, Context.Empty)).Value!.Entry!;
            Assert.Equal(builtIn.RepeatRuleId, savedBuiltIn.RepeatRuleId);
            Assert.Equal(CalendarRepeatPattern.Weekly, savedBuiltIn.RepeatPattern);
            var builtInDates = await client.ListEventsAsync(new() { StartDate = request.Date, EndDate = new(2026, 11, 30) }, Context.Empty);
            Assert.True(builtInDates.IsSuccess, builtInDates.Message);
            Assert.Equal(new[] { request.Date, request.Date.AddDays(7) }, builtInDates.Value!.Select(item => item.Date));
        });
    }

    [Fact]
    public async Task SchedulingMigrationPreservesExistingRecurrenceAndDefaultsToNoAdjustment()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await Migrate(connection, 3);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO OfficialCalendarEntries (Id, Title, Notes, Kind, Date, StartTime, Style, CreatedAt, UpdatedAt, TenantId, RepeatPattern, RepeatInterval, RepeatCount) VALUES ('series', 'Existing', 'Keep', 0, '2026-11-06', '09:00:00', 'blue', '2026-11-06T00:00:00Z', '2026-11-06T00:00:00Z', 'tenant', 3, 2, 10)";
        await command.ExecuteNonQueryAsync();
        command.CommandText = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "calendar-migrations/004_scheduling_extensions.sql"));
        await command.ExecuteNonQueryAsync();
        command.CommandText = "SELECT Id, Title, RepeatPattern, RepeatInterval, RepeatCount, RepeatRuleId, AdjustmentRuleId FROM OfficialCalendarEntries";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("series", reader.GetString(0));
        Assert.Equal("Existing", reader.GetString(1));
        Assert.Equal(3, reader.GetInt32(2));
        Assert.Equal(2, reader.GetInt32(3));
        Assert.Equal(10, reader.GetInt32(4));
        Assert.True(reader.IsDBNull(5) && reader.IsDBNull(6));
    }

    private static async Task Migrate(SqliteConnection connection, int count = int.MaxValue)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "calendar-migrations"), "*.sql").Order(StringComparer.Ordinal).Take(count))
        {
            await using var migration = connection.CreateCommand();
            migration.CommandText = await File.ReadAllTextAsync(file);
            await migration.ExecuteNonQueryAsync();
        }
    }
}
