using NOF.Contract;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class ListCalendarEntries(CalendarEntryOperations operations) : CalendarRpcService.ListEntries
{
    public override Task<Result<CalendarEntryDetails[]>> HandleAsync(ListCalendarEntriesRequest request, Context context, CancellationToken cancellationToken) => operations.ListAsync(request, request.Kind, cancellationToken);
}
