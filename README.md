# PluginHost

> MEF-based plugin loader that scans a /plugins folder and exposes a typed API to loaded assemblies

## What it does
Defines an `IPlugin` contract, then at runtime scans a `/plugins` directory for DLLs that export implementations. Loads them via MEF (Managed Extensibility Framework), calls their lifecycle methods, and provides a live reload mechanism — drop a new DLL in and the host picks it up without restarting.

## Quick Start
```bash
git clone https://github.com/MrHassan2027/PluginHost
cd PluginHost
dotnet build
# Copy a plugin DLL into ./plugins/
dotnet run --project PluginHost.Demo
```

```csharp
// Implementing a plugin (separate project):
[Export(typeof(IPlugin))]
public class MyPlugin : IPlugin
{
    public string Name => "MyPlugin";
    public void OnLoad(IPluginContext ctx) => ctx.Log("Hello from MyPlugin!");
    public void OnUnload() { }
}
```

## Features
- `IPlugin` interface with `OnLoad` / `OnUnload` lifecycle
- `IPluginContext` gives plugins access to logging, config, and a shared event bus
- Hot-reload: `FileSystemWatcher` on `/plugins` triggers re-compose
- Isolation: each plugin assembly loaded in its own `AssemblyLoadContext`
- Plugin metadata via `[PluginMeta(Name, Version, Author)]` attribute

## Tech Stack
| Tool | Why |
|------|-----|
| C# / .NET 8 | `AssemblyLoadContext` for isolation |
| MEF (`System.Composition`) | Export/Import discovery |
| `FileSystemWatcher` | Drop-in hot reload |

## Architecture
```
PluginHost/
├── PluginHost.Contracts/   # IPlugin, IPluginContext, attributes
├── PluginHost.Host/        # Loader, MEF container, hot-reload watcher
├── PluginHost.Demo/        # Sample host console app
└── SamplePlugin/           # Example plugin DLL
```
