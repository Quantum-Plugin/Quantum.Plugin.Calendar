using NOF.Application;
using NOF.Contract;
using NOF.Domain;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class CalendarEntryOperations(IRepository<CalendarEntry> entries, IDbContext database, TimeProvider timeProvider, CalendarChangeNotifications notifications, CalendarSchedulingExtensions scheduling)
{
    public async Task<Result<CalendarEntryDetails>> CreateAsync(SaveCalendarEntryRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);
        var item = CalendarEntry.Create(request.Title, request.Notes, request.Kind, request.Date, request.StartTime, request.EndTime, request.Style, timeProvider);
        SetRecurrence(item, request);
        item.SetAdjustment(request.AdjustmentRuleId);
        CalendarScheduleResolver.Validate(Details(item), scheduling.Capture());
        await entries.AddAsync(item, cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync(Notification(item, "created"), item.Id);
        return Details(item);
    }

    public async Task<Result<CalendarEntryDetails>> UpdateAsync(Guid id, SaveCalendarEntryRequest? request, Context context, CancellationToken cancellationToken, CalendarEntryKind? kind = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null)
        {
            throw new DomainException("calendar_invalid_request", "Entry is required.");
        }

        var item = await FindAsync(id, kind, context, cancellationToken);
        var candidate = CalendarEntry.Create(request.Title, request.Notes, request.Kind, request.Date, request.StartTime, request.EndTime, request.Style, timeProvider);
        SetRecurrence(candidate, request);
        candidate.SetAdjustment(request.AdjustmentRuleId);
        CalendarScheduleResolver.Validate(Details(candidate), scheduling.Capture());
        item.Update(request.Title, request.Notes, request.Kind, request.Date, request.StartTime, request.EndTime, request.Style, timeProvider.GetUtcNow(), request.RepeatPattern, request.RepeatInterval, request.RepeatWeekdays, request.RepeatWeekOfMonth, request.RepeatUntil, request.RepeatCount, request.RepeatRuleId, request.AdjustmentRuleId);
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync(Notification(item, "updated"), item.Id);
        return Details(item);
    }

    public async Task<QuantumRequestResult<CalendarEntryLookup>> GetAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken, CalendarEntryKind? kind = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = entries.AsNoTracking().Where(item => item.Id == request.Id);
        if (kind is not null)
        {
            query = query.Where(item => item.Kind == kind);
        }

        var item = await query.ProjectTo<CalendarEntryDetails>().SingleOrDefaultAsync(cancellationToken);
        if (item is null && context[QuantumRequestContext.Dispatched] is true)
        {
            return QuantumRequestResult<CalendarEntryLookup>.Pass();
        }

        return QuantumRequestResult<CalendarEntryLookup>.Answer(new CalendarEntryLookup { Entry = item is null ? null : item with { SeriesStartDate = item.Date } });
    }

    public async Task<Result> DeleteAsync(CalendarEntryRequest request, Context context, CancellationToken cancellationToken, CalendarEntryKind? kind = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = await FindAsync(request.Id, kind, context, cancellationToken);
        entries.Remove(item);
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync(Notification(item, "deleted"), item.Id);
        return Result.Success();
    }

    public async Task<Result<CalendarEntryDetails>> SetCompletedAsync(SetCalendarEntryCompletedRequest request, Context context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var item = await FindAsync(request.Id, CalendarEntryKind.Task, context, cancellationToken);
        item.SetCompleted(request.IsCompleted, timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        await notifications.PublishAsync(Notification(item, "updated"), item.Id);
        return Details(item);
    }

    public async Task<Result<CalendarEntryDetails[]>> ListAsync(ListCalendarEntriesRequest request, CalendarEntryKind? kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request.EndDate < request.StartDate)
        {
            throw new DomainException("calendar_invalid_request", "End date cannot be earlier than start date.");
        }

        if (request.SeriesPage is < 0 or > 100000)
        {
            throw new DomainException("calendar_invalid_request", "The stored-series page is invalid.");
        }

        if (request.SeriesPage is null && request.EndDate.DayNumber - request.StartDate.DayNumber >= 3660)
        {
            throw new DomainException("calendar_invalid_request", "A calendar query cannot exceed 3,660 days.");
        }

        if (kind is not null && !Enum.IsDefined(kind.Value))
        {
            throw new DomainException("calendar_invalid_request", "Invalid entry type.");
        }

        if (request.SeriesPage is { } page)
        {
            var stored = entries.AsNoTracking().Where(item => item.Date >= request.StartDate && item.Date <= request.EndDate);
            if (kind is not null)
            {
                stored = stored.Where(item => item.Kind == kind);
            }

            var series = await stored.OrderBy(item => item.Date).ThenBy(item => item.Id).Skip(page * 100).Take(100).ProjectTo<CalendarEntryDetails>().ToArrayAsync(cancellationToken);
            return series.Select(item => item with { SeriesStartDate = item.Date }).ToArray();
        }

        var snapshot = scheduling.Capture();
        var query = entries.AsNoTracking().Where(item => item.AdjustmentRuleId != null || (item.Date <= request.EndDate && (item.Date >= request.StartDate || item.RepeatPattern != CalendarRepeatPattern.None || item.RepeatRuleId != null) && (item.RepeatUntil == null || item.RepeatUntil >= request.StartDate)));
        if (kind is not null)
        {
            query = query.Where(item => item.Kind == kind);
        }

        var candidates = await query.ProjectTo<CalendarEntryDetails>().ToArrayAsync(cancellationToken);
        var occurrences = new List<CalendarEntryDetails>();
        foreach (var item in candidates)
        {
            foreach (var occurrence in CalendarScheduleResolver.Expand(item, snapshot, request.StartDate, request.EndDate, cancellationToken))
            {
                if (occurrences.Count >= 10000)
                {
                    throw new DomainException("calendar_invalid_request", "Too many calendar occurrences. Use a shorter date range.");
                }

                occurrences.Add(occurrence);
            }
        }

        return occurrences.OrderBy(item => item.Date).ThenBy(item => item.StartTime).ThenBy(item => item.Title).ThenBy(item => item.Id).ThenBy(item => item.OriginalDate).ToArray();
    }

    public async Task<Result<CalendarEntryDetails[]>> ListTasksAsync(ListCalendarTasksRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = entries.AsNoTracking().Where(item => item.Kind == CalendarEntryKind.Task);
        if (!request.IncludeCompleted)
        {
            query = query.Where(item => !item.IsCompleted);
        }

        var items = await query.ProjectTo<CalendarEntryDetails>().ToArrayAsync(cancellationToken);
        var snapshot = scheduling.Capture();
        return items.SelectMany(item => CalendarScheduleResolver.Expand(item, snapshot, DateOnly.MinValue, DateOnly.MaxValue, cancellationToken)).OrderBy(item => item.IsCompleted).ThenBy(item => item.Date).ThenBy(item => item.StartTime).ThenBy(item => item.Title).ThenBy(item => item.Id).ToArray();
    }

    private async Task<CalendarEntry> FindAsync(Guid id, CalendarEntryKind? kind, Context context, CancellationToken cancellationToken)
    {
        var query = entries.Where(item => item.Id == id);
        if (kind is not null)
        {
            query = query.Where(item => item.Kind == kind);
        }

        return await query.SingleOrDefaultAsync(cancellationToken) ?? throw new DomainException(context[QuantumRequestContext.Dispatched] is true ? QuantumRequestContext.PassCode : "calendar_entry_not_found", "The calendar item was not found.");
    }

    private static CalendarEntryDetails Details(CalendarEntry item) => Mapper.Current.Map<CalendarEntry, CalendarEntryDetails>(item) with { SeriesStartDate = item.Date };
    private static string Notification(CalendarEntry item, string action) => $"calendar.{(item.Kind == CalendarEntryKind.Event ? "event" : "task")}.{action}";
    private static void SetRecurrence(CalendarEntry item, SaveCalendarEntryRequest request) => item.SetRecurrence(request.RepeatPattern, request.RepeatInterval, request.RepeatWeekdays, request.RepeatWeekOfMonth, request.RepeatUntil, request.RepeatCount, request.RepeatRuleId);
}
