namespace PluginHost.Contracts;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    void OnLoad(IPluginContext ctx);
    void OnUnload();
}

public interface IPluginContext
{
    void Log(string message);
    string? GetConfig(string key);
    void Publish(string eventName, object? data = null);
    void Subscribe(string eventName, Action<object?> handler);
}
