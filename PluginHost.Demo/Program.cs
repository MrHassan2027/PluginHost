using PluginHost.Host;

var context = new PluginContext(new Dictionary<string, string>
{
    ["app.name"] = "PluginHost Demo",
    ["app.version"] = "1.0",
});

using var loader = new PluginLoader("./plugins", context);
loader.LoadAll();
loader.WatchForChanges();

Console.WriteLine("PluginHost running. Drop DLLs into ./plugins to load them. Press Q to quit.");
while (Console.ReadKey(true).Key != ConsoleKey.Q) { }
