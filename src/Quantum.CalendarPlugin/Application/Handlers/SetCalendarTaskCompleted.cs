using NOF.Contract;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class SetCalendarTaskCompleted(CalendarEntryOperations operations) : CalendarRpcService.SetTaskCompleted
{
    public override Task<Result<CalendarEntryDetails>> HandleAsync(SetCalendarEntryCompletedRequest request, Context context, CancellationToken cancellationToken) => operations.SetCompletedAsync(request, context, cancellationToken);
}
