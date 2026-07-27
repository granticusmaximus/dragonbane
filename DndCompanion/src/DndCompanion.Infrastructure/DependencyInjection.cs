using DndCompanion.Application.Abstractions;
using DndCompanion.Infrastructure.Dice;
using DndCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DndCompanion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string sqlitePath)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={sqlitePath}"));
        services.AddSingleton<IDiceRoller, DiceRoller>();
        // TODO: register ITranscriptionService (Whisper.net) and INoteStructurer (Ollama)
        //       in Phase 4/5.
        return services;
    }
}
