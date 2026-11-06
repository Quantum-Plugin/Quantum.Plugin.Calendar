using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class DeleteCalendarEvent(CalendarEntryOperations operations) : CalendarRpcService.DeleteEvent
{
    public override Task<Result> HandleAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken) => operations.DeleteAsync(request, context, cancellationToken, CalendarEntryKind.Event);
}
