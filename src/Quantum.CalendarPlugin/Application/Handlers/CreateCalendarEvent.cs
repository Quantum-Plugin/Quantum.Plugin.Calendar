using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class CreateCalendarEvent(CalendarEntryOperations operations) : CalendarRpcService.CreateEvent
{
    public override Task<Result<CalendarEntryDetails>> HandleAsync(SaveCalendarEventRequest request, Context context, CancellationToken cancellationToken) => operations.CreateAsync(request.ToEntry(), cancellationToken);
}
