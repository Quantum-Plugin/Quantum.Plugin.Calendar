using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class CreateCalendarTask(CalendarEntryOperations operations) : CalendarRpcService.CreateTask
{
    public override Task<Result<CalendarEntryDetails>> HandleAsync(SaveCalendarTaskRequest request, Context context, CancellationToken cancellationToken) => operations.CreateAsync(request.ToEntry(), cancellationToken);
}
