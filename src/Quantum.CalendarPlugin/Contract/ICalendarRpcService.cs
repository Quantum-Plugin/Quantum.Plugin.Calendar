using NOF.Contract;
using Quantum.Plugin.Abstraction;
using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

[Description("Manage events and tasks in the official Quantum calendar.")]
[TransportOverQuantum]
public interface ICalendarRpcService : IRpcService
{
    [Description("List events and tasks within the inclusive date range.")]
    [RequestCapability("calendar.list", QuantumDispatch.All)]
    Result<CalendarEntryDetails[]> ListEntries(ListCalendarEntriesRequest request);
    [Description("List all tasks, optionally excluding completed tasks.")]
    [RequestCapability("calendar.list-tasks", QuantumDispatch.All)]
    Result<CalendarEntryDetails[]> ListTasks(ListCalendarTasksRequest request);
    [Description("Get a calendar item by ID.")]
    [RequestCapability("calendar.get", QuantumDispatch.All)]
    QuantumRequestResult<CalendarEntryLookup> GetEntry(CalendarEntryRequest request);
    [Description("Create an event or task.")]
    [RequestCapability("calendar.create", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result<CalendarEntryDetails> CreateEntry(SaveCalendarEntryRequest request);
    [Description("Replace the contents of a calendar item.")]
    [RequestCapability("calendar.update", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result<CalendarEntryDetails> UpdateEntry(UpdateCalendarEntryRequest request);
    [Description("Set the completion state of a task.")]
    [RequestCapability("calendar.set-completed", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result<CalendarEntryDetails> SetCompleted(SetCalendarEntryCompletedRequest request);
    [Description("Permanently delete a calendar item.")]
    [RequestCapability("calendar.delete", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result DeleteEntry(CalendarEntryRequest request);
    [Description("Refresh the published mobile page from current desktop data. Reopen or refresh the mobile view afterwards to display the new snapshot.")]
    [RequestCapability("calendar.mobile-refresh", QuantumDispatch.Ordered, UniqueOwner = true, RequireSingleProvider = true)]
    Result RefreshMobilePage(RefreshCalendarMobilePageRequest request);
}
