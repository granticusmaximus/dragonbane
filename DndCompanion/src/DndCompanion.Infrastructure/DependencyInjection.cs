using DndCompanion.Application.Abstractions;
using DndCompanion.Infrastructure.Audio;
using DndCompanion.Infrastructure.Dice;
using DndCompanion.Infrastructure.Models;
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

        services.AddSingleton<IAppPaths>(appPaths);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={sqlitePath}"));
        services.AddSingleton<IDiceRoller, DiceRoller>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<ICampaignRepository, EfCampaignRepository>();
        services.AddScoped<ICharacterRepository, EfCharacterRepository>();
        services.AddScoped<IEncounterRepository, EfEncounterRepository>();
        services.AddScoped<SrdImporter>();
        // Singletons: each wraps a native resource (audio device / loaded GGML model) or a
        // long-running model download that must outlive any one Blazor circuit.
        services.AddSingleton<ILocalModelStore, LocalModelStore>(sp => new LocalModelStore(sp.GetRequiredService<IAppPaths>()));
        services.AddSingleton<IAudioRecorder, PortAudioRecorder>();
        services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
        services.AddSingleton<INoteStructurer, LlamaNoteStructurer>();
        return services;
    }
}
