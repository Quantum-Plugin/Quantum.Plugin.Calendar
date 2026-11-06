using Microsoft.Extensions.Logging;
using Quantum.OfficialPlugins.Calendar.Domain;
using Quantum.Plugin.Abstraction;
using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;

namespace Quantum.OfficialPlugins.Calendar;

public sealed class CalendarSchedulingExtensions(IQuantumExtensions? extensions = null, ILogger<CalendarSchedulingExtensions>? logger = null) : IDisposable
{
    public const string RecurrencePoint = "quantum.plugin.calendar/recurrence-rules";
    public const string AdjustmentPoint = "quantum.plugin.calendar/date-adjustments";
    public const int ContractVersion = 1;
    private readonly object _gate = new();
    private readonly List<IDisposable> _handles = [];
    private CalendarScheduleSnapshot _snapshot = extensions is null ? CalendarBuiltInRecurrence.Snapshot : CalendarScheduleSnapshot.Empty;
    private int _generation;
    public event Action? Changed;

    public CalendarScheduleSnapshot Capture()
    {
        lock (_gate)
        {
            return _snapshot;
        }
    }

    public void Start()
    {
        if (extensions is null)
        {
            lock (_gate)
            {
                _snapshot = CalendarBuiltInRecurrence.Snapshot;
            }

            return;
        }

        if (_handles.Count != 0)
        {
            return;
        }

        try
        {
            var generation = ++_generation;
            _handles.Add(extensions.Define("recurrence-rules", ContractVersion));
            _handles.Add(extensions.Define("date-adjustments", ContractVersion));
            foreach (var rule in CalendarBuiltInRecurrence.Rules.Values.OrderBy(rule => rule.Order))
            {
                var payload = JsonSerializer.SerializeToElement(new Dictionary<string, object>
                {
                    ["title"] = rule.Title,
                    ["pattern"] = rule.Pattern.ToString()!,
                    ["interval"] = rule.Interval
                }, new JsonSerializerOptions());
                _handles.Add(extensions.Register(RecurrencePoint, ContractVersion, rule.Id.Split('/')[1], payload, rule.Order));
            }

            _handles.Add(extensions.Observe(RecurrencePoint, ContractVersion, snapshot => Update(snapshot, generation)));
            _handles.Add(extensions.Observe(AdjustmentPoint, ContractVersion, snapshot => Update(snapshot, generation)));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _generation++;
            _snapshot = CalendarScheduleSnapshot.Empty;
        }

        for (var index = _handles.Count - 1; index >= 0; index--)
        {
            _handles[index].Dispose();
        }

        _handles.Clear();
    }

    private void Update(QuantumExtensionSnapshot snapshot, int generation)
    {
        var repeats = new Dictionary<string, CalendarRepeatRule>(StringComparer.Ordinal);
        var adjustments = new Dictionary<string, CalendarDateAdjustment>(StringComparer.Ordinal);
        if (snapshot.IsAvailable)
        {
            foreach (var contribution in snapshot.Contributions)
            {
                var id = $"{contribution.PluginId}/{contribution.LocalId}";
                try
                {
                    CalendarScheduleSelection.ValidateId(id);
                    var value = contribution.Value;
                    var title = value.GetProperty("title").GetString();
                    if (string.IsNullOrWhiteSpace(title) || title.Length > 120)
                    {
                        throw new FormatException();
                    }

                    if (snapshot.PointId == RecurrencePoint)
                    {
                        if (value.TryGetProperty("pattern", out var patternValue))
                        {
                            var text = patternValue.GetString();
                            var interval = value.TryGetProperty("interval", out var intervalValue) ? intervalValue.GetInt32() : 1;
                            if (value.TryGetProperty("dates", out _) || !Enum.TryParse<CalendarRepeatPattern>(text, out var pattern) || !Enum.IsDefined(pattern) || pattern == CalendarRepeatPattern.None || pattern.ToString() != text || interval is < 1 or > 999 || (pattern == CalendarRepeatPattern.Weekdays && interval != 1))
                            {
                                throw new FormatException();
                            }

                            repeats.Add(id, new(id, title, Array.Empty<DateOnly>(), pattern, interval, contribution.Order));
                        }
                        else
                        {
                            var dates = value.GetProperty("dates").EnumerateArray().Select(Date).Order().ToArray();
                            if (value.TryGetProperty("interval", out _) || dates.Length is < 1 or > 10000 || dates.Distinct().Count() != dates.Length)
                            {
                                throw new FormatException();
                            }

                            repeats.Add(id, new(id, title, Array.AsReadOnly(dates), Order: contribution.Order));
                        }
                    }
                    else
                    {
                        var from = Date(value.GetProperty("validFrom"));
                        var until = Date(value.GetProperty("validUntil"));
                        var changes = new Dictionary<DateOnly, DateOnly?>();
                        foreach (var change in value.GetProperty("changes").EnumerateArray())
                        {
                            var source = Date(change.GetProperty("date"));
                            var target = change.GetProperty("targetDate");
                            DateOnly? destination = target.ValueKind == JsonValueKind.Null ? null : Date(target);
                            if (source < from || source > until || destination < from || destination > until || changes.Count >= 10000)
                            {
                                throw new FormatException();
                            }

                            changes.Add(source, destination);
                        }

                        if (from > until)
                        {
                            throw new FormatException();
                        }

                        adjustments.Add(id, new(id, title, from, until, changes.ToFrozenDictionary()));
                    }
                }
                catch (Exception exception) when (exception is FormatException or InvalidOperationException or KeyNotFoundException or ArgumentException or NOF.Domain.DomainException)
                {
                    logger?.LogWarning("Ignored invalid calendar scheduling contribution {ContributionId} at {PointId}.", id, snapshot.PointId);
                }
            }
        }

        lock (_gate)
        {
            if (generation != _generation)
            {
                return;
            }

            _snapshot = snapshot.PointId == RecurrencePoint
                ? _snapshot with { RepeatRules = repeats.ToFrozenDictionary(StringComparer.Ordinal) }
                : _snapshot with { Adjustments = adjustments.ToFrozenDictionary(StringComparer.Ordinal) };
        }

        Changed?.Invoke();
    }

    private static DateOnly Date(JsonElement element) => DateOnly.ParseExact(element.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
