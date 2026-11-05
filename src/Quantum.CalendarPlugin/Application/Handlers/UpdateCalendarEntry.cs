using NOF.Application;
using NOF.Contract;
using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class UpdateCalendarEntry(IRepository<CalendarEntry> entries, IDbContext database, TimeProvider timeProvider, CalendarChangeNotifications notifications) : CalendarRpcService.UpdateEntry
{
    public override async Task<Result<CalendarEntryDetails>> HandleAsync(UpdateCalendarEntryRequest request, Context context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Entry is null)
        {
            throw new DomainException("calendar_invalid_request", "Entry is required.");
        }

        var item = await entries.Where(item => item.Id == request.Id).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            var code = context[QuantumRequestContext.Dispatched] is true ? QuantumRequestContext.PassCode : "calendar_entry_not_found";
            throw new DomainException(code, "The calendar item was not found.");
        }

        var replacement = request.Entry;
        item.Update(replacement.Title, replacement.Notes, replacement.Kind, replacement.Date, replacement.StartTime, replacement.EndTime, replacement.Style, timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync("calendar.updated", item.Id);
        return Mapper.Current.Map<CalendarEntry, CalendarEntryDetails>(item);
    }
}
