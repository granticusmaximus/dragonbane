using DndCompanion.Application.Abstractions;
using DndCompanion.Infrastructure.Audio;
using DndCompanion.Infrastructure.Dice;
using DndCompanion.Infrastructure.Notes;
using DndCompanion.Infrastructure.Persistence;
using DndCompanion.Infrastructure.Srd;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DndCompanion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string dataDirectory)
    {
        var appPaths = new AppPaths(dataDirectory);
        var sqlitePath = Path.Combine(appPaths.DataDirectory, "dndcompanion.db");
        var whisperModelPath = Path.Combine(appPaths.DataDirectory, "models", "ggml-base.bin");

        services.AddSingleton<IAppPaths>(appPaths);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={sqlitePath}"));
        services.AddSingleton<IDiceRoller, DiceRoller>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<ICampaignRepository, EfCampaignRepository>();
        services.AddScoped<ICharacterRepository, EfCharacterRepository>();
        services.AddScoped<SrdImporter>();
        // Both wrap a single native resource (audio device / loaded GGML model) meant to
        // live for the app's lifetime, not per-request.
        services.AddSingleton<IAudioRecorder, PortAudioRecorder>();
        services.AddSingleton<ITranscriptionService>(_ => new WhisperTranscriptionService(whisperModelPath));
        // Assumes a local `ollama serve` already running with the model pulled — see README.
        services.AddSingleton<INoteStructurer>(_ => new OllamaNoteStructurer());
        return services;
    }
}
