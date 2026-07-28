namespace DndCompanion.Application.Abstractions;

/// <summary>
/// Root directory for persistent app data (DB, downloaded models, recordings) — stable
/// regardless of how the Host was launched. Deliberately NOT AppContext.BaseDirectory:
/// that's the build output folder, which differs between `dotnet run`
/// (bin/Debug/net10.0) and `electronize start` (obj/Host/bin), so anything keyed off it
/// silently forked into two different data sets depending on launch method.
/// </summary>
public interface IAppPaths
{
    string DataDirectory { get; }
}
