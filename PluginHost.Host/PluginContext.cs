using PluginHost.Contracts;

namespace PluginHost.Host;

public class PluginContext : IPluginContext
{
    private readonly Dictionary<string, List<Action<object?>>> _subscribers = new();
    private readonly Dictionary<string, string> _config;

    public PluginContext(Dictionary<string, string>? config = null)
        => _config = config ?? new();

    public void Log(string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Plugin] {message}");

    public string? GetConfig(string key) =>
        _config.TryGetValue(key, out var v) ? v : null;

    public void Publish(string eventName, object? data = null)
    {
        if (!_subscribers.TryGetValue(eventName, out var handlers)) return;
        foreach (var h in handlers) h(data);
    }

    public void Subscribe(string eventName, Action<object?> handler)
    {
        if (!_subscribers.ContainsKey(eventName)) _subscribers[eventName] = new();
        _subscribers[eventName].Add(handler);
    }
}
