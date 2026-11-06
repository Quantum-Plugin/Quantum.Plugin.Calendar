using NOF.Contract;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class GetCalendarEntry(CalendarEntryOperations operations) : CalendarRpcService.GetEntry
{
    public override Task<QuantumRequestResult<CalendarEntryLookup>> HandleAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken) => operations.GetAsync(request, context, cancellationToken);
}
