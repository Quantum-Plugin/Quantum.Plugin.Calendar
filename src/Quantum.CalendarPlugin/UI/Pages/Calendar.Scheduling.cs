using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar
{
    private readonly CalendarSchedulingExtensions? _scheduling;
    private bool _disposed;
    private bool _schedulingRefreshPending;
    private bool _showSeries;
    private int _seriesPage;
    private CalendarEntryDetails[] _seriesItems = [];
    private CalendarScheduleSnapshot SchedulingSnapshot => _scheduling?.Capture() ?? CalendarBuiltInRecurrence.Snapshot;

    private Task ToggleSeriesAsync()
    {
        _showSeries = !_showSeries;
        return _showSeries ? ExecuteAsync(LoadSeriesCoreAsync) : Task.CompletedTask;
    }

    private Task ChangeSeriesPageAsync(int offset) => ExecuteAsync(async () =>
    {
        _seriesPage += offset;
        await LoadSeriesCoreAsync();
    });

    private async Task LoadSeriesCoreAsync()
    {
        var result = await _calendarService.ListEntriesAsync(new() { StartDate = DateOnly.MinValue, EndDate = DateOnly.MaxValue, SeriesPage = _seriesPage }, Context.Empty);
        if (IsSuccessful(result))
        {
            _seriesItems = result.Value!;
        }
        else
        {
            _seriesItems = [];
        }
    }

    private void SchedulingChanged()
    {
        if (!_disposed)
        {
            _ = InvokeAsync(async () =>
            {
                if (!_disposed)
                {
                    if (_isBusy)
                    {
                        _schedulingRefreshPending = true;
                    }
                    else
                    {
                        await ExecuteAsync(LoadAllCoreAsync);
                    }

                    StateHasChanged();
                }
            });
        }
    }
}
