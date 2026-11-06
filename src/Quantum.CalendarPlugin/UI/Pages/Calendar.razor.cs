using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;
using System.Globalization;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private ElementReference _editorDialog;
    private IJSObjectReference? _editorModule;
    private readonly string _editorTitleId = $"calendar-editor-{Guid.NewGuid():N}";
    private readonly ICalendarRpcServiceClient _calendarService;
    private static readonly string[] Styles = ["violet", "blue", "coral", "green"];
    public Calendar(ICalendarRpcServiceClient calendarService, Quantum.Plugin.Abstraction.IQuantumRequester? invoker = null, CalendarSchedulingExtensions? scheduling = null)
    {
        _calendarService = calendarService;
        _invoker = invoker;
        _scheduling = scheduling;
    }

    private static IReadOnlyList<string> Weekdays
    {
        get
        {
            var names = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;
            return [.. names.Skip(1), names[0]];
        }
    }

    private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<CalendarEntryDetails> _items = [];
    private IReadOnlyList<CalendarEntryDetails> _tasks = [];
    private DateOnly _visibleMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);
    private EntryDraft _draft = EntryDraft.Create(DateOnly.FromDateTime(DateTime.Today));
    private TaskFilter _taskFilter = TaskFilter.Open;
    private IReadOnlyList<CalendarDay>? _previousMonthDays;
    private int _monthDirection = 1;
    private Guid? _editingId;
    private (Guid Id, bool IsTaskList)? _pendingDelete;
    private bool _isEditorOpen;
    private bool _isBusy;
    private string? _errorMessage;
    private string MonthTitle => _visibleMonth.ToDateTime(TimeOnly.MinValue).ToString("Y", CultureInfo.CurrentCulture);
    private string SelectedDateTitle => _selectedDate.ToDateTime(TimeOnly.MinValue).ToString("MMMM dddd", CultureInfo.CurrentCulture);
    private string EditorTitle => _editingId is null ? L["New item"] : L["Edit item"];
    private IReadOnlyList<CalendarEntryDetails> SelectedItems => [.. _items.Where(item => item.Date == _selectedDate)];
    private IEnumerable<CalendarEntryDetails> VisibleItems => _items.Where(item => item.Date >= VisibleStartDate && item.Date <= VisibleEndDate);
    private int EventCount => VisibleItems.Count(item => item.Kind == CalendarEntryKind.Event);
    private int OpenTaskCount => VisibleItems.Count(item => item.Kind == CalendarEntryKind.Task && !item.IsCompleted);
    private int AllOpenTaskCount => _tasks.Count(task => !task.IsCompleted);
    private int FreeDayCount => (VisibleEndDate.DayNumber - VisibleStartDate.DayNumber + 1) - VisibleItems.Select(item => item.Date).Distinct().Count();
    private IReadOnlyList<CalendarEntryDetails> FilteredTasks => [.._tasks.Where(task => _taskFilter switch
    {
        TaskFilter.Open => !task.IsCompleted,
        TaskFilter.Completed => task.IsCompleted,
        _ => true
    })];

    private IReadOnlyList<CalendarDay> CalendarDays
    {
        get
        {
            var firstDay = new DateOnly(_visibleMonth.Year, _visibleMonth.Month, 1);
            var mondayOffset = ((int)firstDay.DayOfWeek + 6) % 7;
            var gridStart = firstDay.AddDays(-mondayOffset);
            return [..Enumerable.Range(0, Math.Min(42, DateOnly.MaxValue.DayNumber - gridStart.DayNumber + 1)).Select(offset =>
            {
                var date = gridStart.AddDays(offset);
                return new CalendarDay
                {
                    Date = date,
                    IsVisibleMonth = date.Month == _visibleMonth.Month && date.Year == _visibleMonth.Year,
                    IsToday = date == _today,
                    Items = [.._items.Where(item => item.Date == date)]
                };
            })];
        }
    }

    protected override Task OnInitializedAsync()
    {
        if (_scheduling is not null)
        {
            _scheduling.Changed += SchedulingChanged;
        }

        return ExecuteAsync(LoadAllCoreAsync);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isEditorOpen && _ledgerViewing is null && !_isWeekView && _editorModule is null)
        {
            return;
        }

        _editorModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/quantum.plugin.calendar/calendar.js");
        await _editorModule.InvokeVoidAsync("syncEditor", _editorDialog, _isEditorOpen);
        await _editorModule.InvokeVoidAsync("syncEditor", _ledgerDialog, _ledgerViewing is not null);
        if (_isWeekView)
        {
            await _editorModule.InvokeVoidAsync("syncWeekViewport", _weekViewport, WeekStartDate.DayNumber, WeekScrollMinute, _selectedDate.DayNumber - WeekStartDate.DayNumber);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_scheduling is not null)
        {
            _scheduling.Changed -= SchedulingChanged;
        }

        if (_editorModule is null)
        {
            return;
        }

        try
        {
            await _editorModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The WebView has already disconnected.
        }
    }

    private void SetVisibleMonth(DateOnly month)
    {
        if (month == _visibleMonth)
        {
            return;
        }

        _previousMonthDays = _isWeekView ? null : CalendarDays;
        _monthDirection = month > _visibleMonth ? 1 : -1;
        _visibleMonth = month;
    }

    private async Task ChangeMonthAsync(int offset)
    {
        SetVisibleMonth(_visibleMonth.AddMonths(offset));
        _selectedDate = new DateOnly(_visibleMonth.Year, _visibleMonth.Month, Math.Min(_selectedDate.Day, DateTime.DaysInMonth(_visibleMonth.Year, _visibleMonth.Month)));
        CancelEdit();
        await LoadVisiblePeriodAsync();
    }

    private async Task ShowTodayAsync()
    {
        SetVisibleMonth(new DateOnly(_today.Year, _today.Month, 1));
        _selectedDate = _today;
        CancelEdit();
        await LoadVisiblePeriodAsync();
    }

    private async Task SelectDateAsync(DateOnly date)
    {
        var previousStart = VisibleStartDate;
        _selectedDate = date;
        CancelEdit();
        CancelDelete();
        if (date.Month != _visibleMonth.Month || date.Year != _visibleMonth.Year)
        {
            SetVisibleMonth(new DateOnly(date.Year, date.Month, 1));
        }

        if (VisibleStartDate != previousStart)
        {
            await LoadVisiblePeriodAsync();
        }
    }

    private void BeginCreate()
    {
        _editingId = null;
        _draft = EntryDraft.Create(_selectedDate);
        _isEditorOpen = true;
        _errorMessage = null;
    }

    private async Task BeginCreateTaskAsync()
    {
        _selectedDate = _today;
        SetVisibleMonth(new DateOnly(_today.Year, _today.Month, 1));
        await ExecuteAsync(LoadVisiblePeriodCoreAsync);
        if (_errorMessage is not null)
        {
            return;
        }

        BeginCreate();
        SetDraftKind(CalendarEntryKind.Task);
    }

    private void BeginEdit(CalendarEntryDetails item)
    {
        _editingId = item.Id;
        _draft = EntryDraft.From(item);
        _isEditorOpen = true;
        _errorMessage = null;
    }

    private async Task EditTaskAsync(CalendarEntryDetails task)
    {
        _selectedDate = task.Date;
        SetVisibleMonth(new DateOnly(task.Date.Year, task.Date.Month, 1));
        await ExecuteAsync(LoadVisiblePeriodCoreAsync);
        if (_errorMessage is not null)
        {
            return;
        }

        BeginEdit(task);
    }

    private void SetDraftKind(CalendarEntryKind kind)
    {
        _draft.Kind = kind;
        if (kind == CalendarEntryKind.Task)
        {
            _draft.EndTime = null;
            _draft.RepeatRuleId = null;
            _draft.RepeatPattern = CalendarRepeatPattern.None;
            _draft.RepeatInterval = 1;
            _draft.RepeatWeekdays = CalendarWeekdays.None;
            _draft.RepeatWeekOfMonth = null;
            _draft.RepeatUntil = null;
            _draft.RepeatCount = null;
        }
    }

    private void CancelEdit()
    {
        _editingId = null;
        _isEditorOpen = false;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_draft.Title))
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            if (_editingId is { } id)
            {
                if (!IsSuccessful(_draft.Kind == CalendarEntryKind.Event
                    ? await _calendarService.UpdateEventAsync(new() { Id = id, Entry = _draft.ToEventRequest() }, Context.Empty)
                    : await _calendarService.UpdateTaskAsync(new() { Id = id, Entry = _draft.ToTaskRequest() }, Context.Empty)))
                {
                    return;
                }
            }
            else
            {
                if (!IsSuccessful(_draft.Kind == CalendarEntryKind.Event
                    ? await _calendarService.CreateEventAsync(_draft.ToEventRequest(), Context.Empty)
                    : await _calendarService.CreateTaskAsync(_draft.ToTaskRequest(), Context.Empty)))
                {
                    return;
                }
            }

            _selectedDate = _isWeekView && _editingId is not null && (_draft.RepeatPattern != CalendarRepeatPattern.None || _draft.RepeatRuleId is not null) ? _selectedDate : _draft.Date;
            SetVisibleMonth(new DateOnly(_selectedDate.Year, _selectedDate.Month, 1));
            CancelEdit();
            await LoadAllCoreAsync();
        });
    }

    private Task ToggleCompletedAsync(CalendarEntryDetails item) => ExecuteAsync(async () =>
    {
        if (!IsSuccessful(await _calendarService.SetTaskCompletedAsync(new() { Id = item.Id, IsCompleted = !item.IsCompleted }, Context.Empty)))
        {
            return;
        }

        await LoadAllCoreAsync();
    });
    private void CancelDelete() => _pendingDelete = null;
    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            CancelDelete();
        }
    }

    private Task DeleteAsync(Guid id, bool isTaskList)
    {
        if (_isBusy)
        {
            return Task.CompletedTask;
        }

        if (_pendingDelete != (id, isTaskList))
        {
            _pendingDelete = (id, isTaskList);
            return Task.CompletedTask;
        }

        CancelDelete();
        return ExecuteAsync(async () =>
        {
            var lookup = await _calendarService.GetEntryAsync(new() { Id = id }, Context.Empty);
            if (!IsSuccessful(lookup) || lookup.Value?.Entry is not { } entry)
            {
                return;
            }

            if (!IsSuccessful(entry.Kind == CalendarEntryKind.Event
                ? await _calendarService.DeleteEventAsync(new() { Id = id }, Context.Empty)
                : await _calendarService.DeleteTaskAsync(new() { Id = id }, Context.Empty)))
            {
                return;
            }

            if (_editingId == id)
            {
                CancelEdit();
            }

            await LoadAllCoreAsync();
        });
    }

    private Task LoadVisiblePeriodAsync() => ExecuteAsync(LoadVisiblePeriodCoreAsync);
    private async Task<bool> LoadVisiblePeriodCoreAsync()
    {
        var result = await _calendarService.ListEntriesAsync(new() { StartDate = VisibleStartDate, EndDate = VisibleEndDate }, Context.Empty);
        if (!IsSuccessful(result))
        {
            _items = [];
            return false;
        }

        _items = result.Value!;
        return true;
    }

    private async Task LoadAllCoreAsync()
    {
        if (_showSeries)
        {
            await LoadSeriesCoreAsync();
        }

        // An occurrence failure must not suppress independent task and recovery reads.
        await LoadVisiblePeriodCoreAsync();
        await RefreshLedgerCapabilityAsync();
        var result = await _calendarService.ListTasksAsync(new() { }, Context.Empty);
        _tasks = IsSuccessful(result) ? result.Value! : [];
    }

    private bool IsSuccessful(IResult result)
    {
        if (result.IsSuccess)
        {
            return true;
        }

        _errorMessage = L[result.Message];
        return false;
    }

    private bool IsOverdue(CalendarEntryDetails task) => !task.IsCompleted && (task.Date < _today || (task.Date == _today && task.StartTime < TimeOnly.FromDateTime(DateTime.Now)));
    private static string FormatDeadline(CalendarEntryDetails task) => $"{task.Date.ToDateTime(task.StartTime):D} · {task.StartTime:HH:mm}";
    private string FilterLabel(TaskFilter filter) => filter switch
    {
        TaskFilter.Open => L["Open"],
        TaskFilter.Completed => L["Completed"],
        _ => L["All"]
    };
    private async Task ExecuteAsync(Func<Task> action)
    {
        _isBusy = true;
        _errorMessage = null;
        try
        {
            await action();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _errorMessage = L[exception.Message];
        }
        finally
        {
            _isBusy = false;
            if (_schedulingRefreshPending && !_disposed)
            {
                _schedulingRefreshPending = false;
                SchedulingChanged();
            }
        }
    }

    private string GetDayClass(CalendarDay day) => string.Join(" ", new[] { "day-cell", day.IsVisibleMonth ? null : "outside-month", day.IsToday ? "today" : null, day.Date == _selectedDate ? "selected" : null }.Where(value => value is not null));
    private string GetDayAriaLabel(CalendarDay day)
    {
        var date = day.Date.ToDateTime(TimeOnly.MinValue).ToString("D", CultureInfo.CurrentCulture);
        return L["{0}, {1} items", date, day.Items.Count];
    }

    private static string FormatTime(CalendarEntryDetails item) => item.EndTime is { } end ? $"{item.StartTime:HH:mm}–{end:HH:mm}" : item.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
    private sealed record CalendarDay
    {
        public required DateOnly Date { get; set; }
        public required bool IsVisibleMonth { get; set; }
        public required bool IsToday { get; set; }
        public required IReadOnlyList<CalendarEntryDetails> Items { get; set; }
    }

    private enum TaskFilter
    {
        Open,
        Completed,
        All
    }

    private sealed partial class EntryDraft
    {
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public CalendarEntryKind Kind { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string Style { get; set; } = "violet";

        public static EntryDraft Create(DateOnly date) => new()
        {
            Date = date,
            StartTime = new TimeOnly(9, 0)
        };
        public static EntryDraft From(CalendarEntryDetails item) => new()
        {
            Title = item.Title,
            Notes = item.Notes,
            Kind = item.Kind,
            Date = item.SeriesStartDate,
            RepeatRuleId = item.RepeatRuleId,
            AdjustmentRuleId = item.AdjustmentRuleId,
            RepeatPattern = item.RepeatPattern,
            RepeatInterval = item.RepeatInterval,
            RepeatWeekdays = item.RepeatWeekdays,
            RepeatWeekOfMonth = item.RepeatWeekOfMonth,
            RepeatUntil = item.RepeatUntil,
            RepeatCount = item.RepeatCount,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            Style = item.Style
        };
    }
}
