using NOF.Contract;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class ListCalendarTasks(CalendarEntryOperations operations) : CalendarRpcService.ListTasks
{
    public override Task<Result<CalendarEntryDetails[]>> HandleAsync(ListCalendarTasksRequest request, Context context, CancellationToken cancellationToken) => operations.ListTasksAsync(request, cancellationToken);
}
