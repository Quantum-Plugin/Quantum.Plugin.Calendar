using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class UpdateCalendarTask(CalendarEntryOperations operations) : CalendarRpcService.UpdateTask
{
    public override Task<Result<CalendarEntryDetails>> HandleAsync(UpdateCalendarTaskRequest request, Context context, CancellationToken cancellationToken) => operations.UpdateAsync(request.Id, request.Entry?.ToEntry(), context, cancellationToken, CalendarEntryKind.Task);
}
