using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Application;
using System.Reflection;
using Xunit;
using CalendarPage = Quantum.OfficialPlugins.Calendar.UI.Pages.Calendar;

namespace Quantum.CalendarPlugin.Tests.UI.Pages;

public sealed class UIFailedResultTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedCalendarSavePreservesEditorAndDoesNotRefresh(bool update)
    {
        var client = DispatchProxy.Create<ICalendarRpcServiceClient, FailedResultClient>();
        var page = new CalendarPage(client);
        Localize<OfficialPlugins.Calendar.UI.Localization.PluginStrings>(page);
        Set(page, "_isEditorOpen", true);
        if (update)
        {
            Set(page, "_editingId", Guid.NewGuid());
        }

        var draft = Get(page, "_draft")!;
        draft.GetType().GetProperty("Title")!.SetValue(draft, "Keep this title");
        await Invoke(page, "SaveAsync");
        Assert.True((bool)Get(page, "_isEditorOpen")!);
        Assert.Equal("Keep this title", draft.GetType().GetProperty("Title")!.GetValue(draft));
        Assert.Equal("Request rejected.", Get(page, "_errorMessage"));
        Assert.Equal(new[] { update ? "UpdateEventAsync" : "CreateEventAsync" }, ((FailedResultClient)(object)client).Calls);
    }

    private static object? Get(object page, string name) => page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page);
    private static void Set(object page, string name, object value) => page.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, value);
    private static Task Invoke(object page, string name) => (Task)page.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(page, null)!;
    private static void Localize<T>(object page)
    {
        var factory = new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
        page.GetType().GetProperty("L", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(page, new StringLocalizer<T>(factory));
    }

    public class FailedResultClient : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            var resultType = targetMethod.ReturnType.GetGenericArguments()[0];
            return typeof(FailedResultClient).GetMethod(nameof(Fail), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(resultType.GetGenericArguments()[0]).Invoke(null, null);
        }

        private static Task<Result<T>> Fail<T>() => Task.FromResult<Result<T>>(Result.Fail("rejected", "Request rejected."));
    }
}
