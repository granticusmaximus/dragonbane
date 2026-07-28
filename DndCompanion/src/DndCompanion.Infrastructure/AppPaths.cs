using DndCompanion.Application.Abstractions;

namespace DndCompanion.Infrastructure;

public sealed class AppPaths : IAppPaths
{
    public AppPaths(string dataDirectory)
    {
        DataDirectory = dataDirectory;
        Directory.CreateDirectory(DataDirectory);
    }

    public string DataDirectory { get; }
}
