using PluginHost.Contracts;

namespace SamplePlugin;

public class HelloPlugin : IPlugin
{
    public string Name => "HelloPlugin";
    public string Version => "1.0.0";

    public void OnLoad(IPluginContext ctx)
    {
        var appName = ctx.GetConfig("app.name") ?? "Unknown";
        ctx.Log($"Hello from HelloPlugin! Running inside: {appName}");

        ctx.Subscribe("ping", data =>
            ctx.Log($"HelloPlugin received ping: {data}")
        );

        ctx.Publish("ping", "Hello from OnLoad!");
    }

    public void OnUnload() =>
        Console.WriteLine("[HelloPlugin] Goodbye!");
}
