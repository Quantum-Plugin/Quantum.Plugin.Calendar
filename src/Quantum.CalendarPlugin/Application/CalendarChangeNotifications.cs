using Microsoft.Extensions.Logging;
using Quantum.Plugin.Abstraction;

namespace Quantum.OfficialPlugins.Calendar.Application;

public sealed class CalendarChangeNotifications(IQuantumNotifier? notifier = null, ILogger<CalendarChangeNotifications>? logger = null)
{
    public async Task PublishAsync(string capabilityName, Guid id)
    {
        if (notifier is null)
        {
            return;
        }

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        try
        {
            await notifier.PublishAsync(capabilityName, new CalendarEntryChanged { Id = id }, deadline.Token).AsTask().WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            logger?.LogWarning(exception, "Could not finish publishing {CapabilityName} for saved calendar item {EntryId}.", capabilityName, id);
        }
    }
}
