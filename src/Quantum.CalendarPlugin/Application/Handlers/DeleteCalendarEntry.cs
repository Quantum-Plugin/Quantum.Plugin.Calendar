using NOF.Application;
using NOF.Contract;
using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class DeleteCalendarEntry(IRepository<CalendarEntry> entries, IDbContext database, CalendarChangeNotifications notifications) : CalendarRpcService.DeleteEntry
{
    public override async Task<Result> HandleAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = await entries.Where(item => item.Id == request.Id).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            var code = context[QuantumRequestContext.Dispatched] is true ? QuantumRequestContext.PassCode : "calendar_entry_not_found";
            throw new DomainException(code, "The calendar item was not found.");
        }

        entries.Remove(item);
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync("calendar.deleted", item.Id);
        return Result.Success();
    }
}
