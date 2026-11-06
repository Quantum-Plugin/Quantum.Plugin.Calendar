using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class GetCalendarEvent(CalendarEntryOperations operations) : CalendarRpcService.GetEvent
{
    public override Task<QuantumRequestResult<CalendarEntryLookup>> HandleAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken) => operations.GetAsync(request, context, cancellationToken, CalendarEntryKind.Event);
}
