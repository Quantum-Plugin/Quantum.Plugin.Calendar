using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class UpdateCalendarEvent(CalendarEntryOperations operations) : CalendarRpcService.UpdateEvent
{
    public override Task<Result<CalendarEntryDetails>> HandleAsync(UpdateCalendarEventRequest request, Context context, CancellationToken cancellationToken) => operations.UpdateAsync(request.Id, request.Entry?.ToEntry(), context, cancellationToken, CalendarEntryKind.Event);
}
