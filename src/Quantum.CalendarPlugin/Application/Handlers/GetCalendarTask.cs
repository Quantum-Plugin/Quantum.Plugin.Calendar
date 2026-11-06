using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class GetCalendarTask(CalendarEntryOperations operations) : CalendarRpcService.GetTask
{
    public override Task<QuantumRequestResult<CalendarEntryLookup>> HandleAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken) => operations.GetAsync(request, context, cancellationToken, CalendarEntryKind.Task);
}
