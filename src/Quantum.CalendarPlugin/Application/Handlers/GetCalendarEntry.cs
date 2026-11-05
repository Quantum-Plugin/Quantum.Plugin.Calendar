using NOF.Application;
using NOF.Contract;
using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class GetCalendarEntry(IRepository<CalendarEntry> entries) : CalendarRpcService.GetEntry
{
    public override async Task<QuantumRequestResult<CalendarEntryLookup>> HandleAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = await entries.AsNoTracking().Where(item => item.Id == request.Id).ProjectTo<CalendarEntryDetails>().SingleOrDefaultAsync(cancellationToken);
        if (item is null && context[QuantumRequestContext.Dispatched] is true)
        {
            return QuantumRequestResult<CalendarEntryLookup>.Pass();
        }

        return QuantumRequestResult<CalendarEntryLookup>.Answer(new CalendarEntryLookup { Entry = item });
    }
}
