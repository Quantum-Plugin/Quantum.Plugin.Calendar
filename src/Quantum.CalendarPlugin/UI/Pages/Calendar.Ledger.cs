using Microsoft.AspNetCore.Components;
using NOF.Contract;
using Quantum.OfficialPlugins.Calendar.Application;
using Quantum.Plugin.Abstraction;
using System.Text.Json;

namespace Quantum.OfficialPlugins.Calendar.UI.Pages;

public partial class Calendar
{
    private readonly IQuantumRequester? _invoker;
    private bool _canViewLedger;
    private CalendarEntryDetails? _ledgerViewing;
    private LinkedLedgerEntry[] _linkedLedgerEntries = [];
    private ElementReference _ledgerDialog;
    private async Task RefreshLedgerCapabilityAsync()
    {
        _canViewLedger = false;
        if (_invoker is null)
        {
            return;
        }

        try
        {
            var catalog = await _invoker.DiscoverCapabilitiesAsync();
            _canViewLedger = catalog.IsSuccess && catalog.Value!.Capabilities.Any(method => method.Name == "ledger.by-calendar-entry");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Optional UI actions remain hidden while the catalog is unavailable.
        }
    }

    private Task ViewLedgerAsync(CalendarEntryDetails item) => ExecuteAsync(async () =>
    {
        await RefreshLedgerCapabilityAsync();
        if (!_canViewLedger)
        {
            _errorMessage = L["Ledger unavailable. Please refresh and try again."];
            return;
        }

        var result = await _invoker!.RequestSingleAsync<LinkedLedgerEntry[]>("ledger.by-calendar-entry", new { calendarEntryId = item.Id }, Context.Empty);
        if (!IsSuccessful(result))
        {
            return;
        }

        _linkedLedgerEntries = result.Value!;
        _ledgerViewing = item;
    });
}
