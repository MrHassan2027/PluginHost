using System.Reflection;
using System.Runtime.Loader;
using PluginHost.Contracts;

namespace PluginHost.Host;

// Shares the Contracts assembly with the host so IsAssignableFrom works correctly
class PluginLoadContext(string dllPath) : AssemblyLoadContext(dllPath, isCollectible: true)
{
    private static readonly string ContractsName =
        typeof(IPlugin).Assembly.GetName().Name!;

    protected override Assembly? Load(AssemblyName name)
    {
        // Always use the host's copy of Contracts — never load a second instance
        if (name.Name == ContractsName)
            return typeof(IPlugin).Assembly;
        return null;
    }
}

public class PluginLoader : IDisposable
{
    private readonly string _pluginsDir;
    private readonly Dictionary<string, (PluginLoadContext ctx, IPlugin plugin)> _loaded = new();
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
        _watcher.Created += (_, e) => { Thread.Sleep(300); TryLoad(e.FullPath); };
        _watcher.Changed += (_, e) => { Thread.Sleep(300); TryUnload(Path.GetFileNameWithoutExtension(e.Name!)); TryLoad(e.FullPath); };
        _watcher.Deleted += (_, e) => TryUnload(Path.GetFileNameWithoutExtension(e.Name!));
    }

    private void TryLoad(string dllPath)
    {
        try
        {
            var alc = new PluginLoadContext(dllPath);
            var asm = alc.LoadFromAssemblyPath(dllPath);
            var pluginType = typeof(IPlugin);
            bool found = false;

            foreach (var type in asm.GetExportedTypes())
            {
                if (!pluginType.IsAssignableFrom(type) || type.IsAbstract) continue;
                var plugin = (IPlugin)Activator.CreateInstance(type)!;
                _loaded[plugin.Name] = (alc, plugin);
                plugin.OnLoad(_context);
                _context.Log($"Loaded plugin: {plugin.Name} v{plugin.Version}");
                found = true;
            }

            if (!found)
            {
                alc.Unload();
                Console.WriteLine($"[PluginLoader] No IPlugin found in {Path.GetFileName(dllPath)}");
            }
        }
        catch (Exception ex) { Console.WriteLine($"[PluginLoader] Failed to load {Path.GetFileName(dllPath)}: {ex.Message}"); }
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
