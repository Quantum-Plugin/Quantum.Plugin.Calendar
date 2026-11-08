using Quantum.OfficialPlugins.Calendar.Domain;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar
{
    private readonly CalendarSchedulingExtensions? _scheduling;
    private bool _disposed;
    private bool _schedulingRefreshPending;
    private CalendarScheduleSnapshot SchedulingSnapshot => _scheduling?.Capture() ?? CalendarBuiltInRecurrence.Snapshot;

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
