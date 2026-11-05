using NOF.Application;
using NOF.Contract;
using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class SetCalendarEntryCompleted(IRepository<CalendarEntry> entries, IDbContext database, TimeProvider timeProvider, CalendarChangeNotifications notifications) : CalendarRpcService.SetCompleted
{
    public override async Task<Result<CalendarEntryDetails>> HandleAsync(SetCalendarEntryCompletedRequest request, Context context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = await entries.Where(item => item.Id == request.Id).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            var code = context[QuantumRequestContext.Dispatched] is true ? QuantumRequestContext.PassCode : "calendar_entry_not_found";
            throw new DomainException(code, "The calendar item was not found.");
        }

        item.SetCompleted(request.IsCompleted, timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync("calendar.updated", item.Id);
        return Mapper.Current.Map<CalendarEntry, CalendarEntryDetails>(item);
    }
}
