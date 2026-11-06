using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class ListCalendarEvents(CalendarEntryOperations operations) : CalendarRpcService.ListEvents
{
    public override Task<Result<CalendarEntryDetails[]>> HandleAsync(ListCalendarEventsRequest request, Context context, CancellationToken cancellationToken) => operations.ListAsync(new() { StartDate = request.StartDate, EndDate = request.EndDate }, CalendarEntryKind.Event, cancellationToken);
}
