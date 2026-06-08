using System.Reflection;
using System.Runtime.Loader;
using PluginHost.Contracts;

namespace PluginHost.Host;

public class PluginLoader : IDisposable
{
    private readonly string _pluginsDir;
    private readonly Dictionary<string, (AssemblyLoadContext ctx, IPlugin plugin)> _loaded = new();
    private readonly PluginContext _context;
    private FileSystemWatcher? _watcher;

    public PluginLoader(string pluginsDir, PluginContext context)
    {
        _pluginsDir = pluginsDir;
        _context = context;
        Directory.CreateDirectory(pluginsDir);
    }

    public void LoadAll()
    {
        foreach (var dll in Directory.GetFiles(_pluginsDir, "*.dll"))
            TryLoad(dll);
    }

    public void WatchForChanges()
    {
        _watcher = new FileSystemWatcher(_pluginsDir, "*.dll")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Created += (_, e) => TryLoad(e.FullPath);
        _watcher.Changed += (_, e) => { TryUnload(Path.GetFileNameWithoutExtension(e.Name!)); TryLoad(e.FullPath); };
        _watcher.Deleted += (_, e) => TryUnload(Path.GetFileNameWithoutExtension(e.Name!));
    }

    private void TryLoad(string dllPath)
    {
        try
        {
            var alc = new AssemblyLoadContext(dllPath, isCollectible: true);
            var asm = alc.LoadFromAssemblyPath(dllPath);

            foreach (var type in asm.GetExportedTypes())
            {
                if (!typeof(IPlugin).IsAssignableFrom(type) || type.IsAbstract) continue;
                var plugin = (IPlugin)Activator.CreateInstance(type)!;
                _loaded[plugin.Name] = (alc, plugin);
                plugin.OnLoad(_context);
                _context.Log($"Loaded plugin: {plugin.Name} v{plugin.Version}");
            }
        }
        catch (Exception ex) { Console.WriteLine($"[PluginLoader] Failed to load {dllPath}: {ex.Message}"); }
    }

    private void TryUnload(string name)
    {
        if (!_loaded.TryGetValue(name, out var entry)) return;
        try { entry.plugin.OnUnload(); } catch { }
        entry.ctx.Unload();
        _loaded.Remove(name);
        _context.Log($"Unloaded plugin: {name}");
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        foreach (var key in _loaded.Keys.ToList()) TryUnload(key);
    }
}
