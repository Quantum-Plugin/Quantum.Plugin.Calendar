using NOF.Contract;
using Quantum.Plugin.Abstraction;
using System.ComponentModel;

namespace Quantum.OfficialPlugins.Calendar.Application;

[Description("Manage calendar events and tasks using resource-specific capabilities.")]
[TransportOverQuantum]
public interface ICalendarRpcService : IRpcService
{
    [Description("List calendar event occurrences within an inclusive range of at most 3660 days.")]
    [RequestCapability("calendar.event.list", QuantumDispatch.All)]
    Result<CalendarEntryDetails[]> ListEvents(ListCalendarEventsRequest request);

    [Description("Get calendar event by ID. Recurring events are read or changed as a whole series.")]
    [RequestCapability("calendar.event.get", QuantumDispatch.All)]
    QuantumRequestResult<CalendarEntryLookup> GetEvent(CalendarEntryRequest request);

    [Description("Create calendar event.")]
    [RequestCapability("calendar.event.create", QuantumDispatch.Ordered, UniqueOwner = true, RequireSingleProvider = true)]
    Result<CalendarEntryDetails> CreateEvent(SaveCalendarEventRequest request);

    [Description("Update calendar event by ID. Recurring events are read or changed as a whole series.")]
    [RequestCapability("calendar.event.update", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result<CalendarEntryDetails> UpdateEvent(UpdateCalendarEventRequest request);

    [Description("Delete calendar event by ID. Recurring events are read or changed as a whole series.")]
    [RequestCapability("calendar.event.delete", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result DeleteEvent(CalendarEntryRequest request);

    [Description("List calendar tasks, optionally excluding completed tasks.")]
    [RequestCapability("calendar.task.list", QuantumDispatch.All)]
    Result<CalendarEntryDetails[]> ListTasks(ListCalendarTasksRequest request);

    [Description("Get a calendar task by ID.")]
    [RequestCapability("calendar.task.get", QuantumDispatch.All)]
    QuantumRequestResult<CalendarEntryLookup> GetTask(CalendarEntryRequest request);

    [Description("Create calendar task.")]
    [RequestCapability("calendar.task.create", QuantumDispatch.Ordered, UniqueOwner = true, RequireSingleProvider = true)]
    Result<CalendarEntryDetails> CreateTask(SaveCalendarTaskRequest request);

    [Description("Replace the contents of a calendar task by ID.")]
    [RequestCapability("calendar.task.update", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result<CalendarEntryDetails> UpdateTask(UpdateCalendarTaskRequest request);

    [Description("Permanently delete a calendar task by ID.")]
    [RequestCapability("calendar.task.delete", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result DeleteTask(CalendarEntryRequest request);

    [Description("List both event occurrences and tasks in an inclusive range of at most 3660 days.")]
    [RequestCapability("calendar.entry.list", QuantumDispatch.All)]
    Result<CalendarEntryDetails[]> ListEntries(ListCalendarEntriesRequest request);

    [Description("Read either an event series or a task by ID.")]
    [RequestCapability("calendar.entry.get", QuantumDispatch.All)]
    QuantumRequestResult<CalendarEntryLookup> GetEntry(CalendarEntryRequest request);

    [Description("Set a task's completion state. Events cannot be completed.")]
    [RequestCapability("calendar.task.set-completed", QuantumDispatch.Ordered, UniqueOwner = true)]
    Result<CalendarEntryDetails> SetTaskCompleted(SetCalendarEntryCompletedRequest request);

    [Description("Refresh the published mobile page from current desktop data.")]
    [RequestCapability("calendar.mobile-page.refresh", QuantumDispatch.Ordered, UniqueOwner = true, RequireSingleProvider = true)]
    Result RefreshMobilePage(RefreshCalendarMobilePageRequest request);
}
