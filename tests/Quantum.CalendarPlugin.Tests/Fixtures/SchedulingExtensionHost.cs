using System.Text.Json;

namespace Quantum.CalendarPlugin.Tests.Fixtures;

internal sealed class SchedulingExtensionHost : IQuantumExtensions
{
    private readonly Dictionary<string, Action<QuantumExtensionSnapshot>> _observers = [];
    private readonly Dictionary<string, QuantumExtensionContribution[]> _contributions = [];
    private readonly Dictionary<string, (string Point, QuantumExtensionContribution Contribution)> _registered = [];
    public List<string> Definitions { get; } = [];
    public List<Action<QuantumExtensionSnapshot>> RetiredObservers { get; } = [];
    public int ObservationCount => _observers.Count;
    public int RegistrationCount => _registered.Count;
    private long _revision;

    public IDisposable Define(string localId, int contractVersion)
    {
        Assert.Equal(1, contractVersion);
        Definitions.Add(localId);
        return new Handle(() => Definitions.Remove(localId));
    }

    public IDisposable Observe(string pointId, int contractVersion, Action<QuantumExtensionSnapshot> observer)
    {
        Assert.Equal(1, contractVersion);
        _observers.Add(pointId, observer);
        Notify(pointId);
        return new Handle(() =>
        {
            RetiredObservers.Add(observer);
            _observers.Remove(pointId);
        });
    }

    public void Publish(string pointId, params QuantumExtensionContribution[] contributions)
    {
        _contributions[pointId] = contributions;
        Notify(pointId);
    }

    private void Notify(string pointId)
    {
        if (_observers.TryGetValue(pointId, out var observer))
        {
            observer(new(pointId, 1, ++_revision, true, [.. _registered.Values.Where(value => value.Point == pointId).Select(value => value.Contribution), .. _contributions.GetValueOrDefault(pointId) ?? []]));
        }
    }

    public static QuantumExtensionContribution Contribution(string pluginId, string localId, string json)
    {
        using var document = JsonDocument.Parse(json);
        return new(pluginId, localId, 0, document.RootElement.Clone());
    }
    public IQuantumContribution Register(string pointId, int contractVersion, string localId, JsonElement value, int order = 0)
    {
        Assert.Equal(1, contractVersion);
        _registered.Add(localId, (pointId, new("quantum.plugin.calendar", localId, order, value.Clone())));
        Notify(pointId);
        return new ContributionHandle(replacement =>
        {
            _registered[localId] = (pointId, _registered[localId].Contribution with { Value = replacement.Clone() });
            Notify(pointId);
        }, () =>
        {
            _registered.Remove(localId);
            Notify(pointId);
        });
    }

    private sealed class ContributionHandle(Action<JsonElement> update, Action dispose) : IQuantumContribution
    {
        private Action? _dispose = dispose;
        public void Update(JsonElement value)
        {
            ObjectDisposedException.ThrowIf(_dispose is null, this);
            update(value);
        }

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }

    private sealed class Handle(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
