using DndCompanion.Application.Abstractions;
using DndCompanion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Persistence;

public sealed class EfEncounterRepository(AppDbContext db) : IEncounterRepository
{
    public async Task<Encounter?> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Encounters.FindAsync([id], ct);

    public async Task<IReadOnlyList<Encounter>> ListAsync(CancellationToken ct = default) =>
        await db.Encounters.AsNoTracking().OrderBy(e => e.CreatedUtc).ToListAsync(ct);

    public async Task AddAsync(Encounter entity, CancellationToken ct = default) =>
        await db.Encounters.AddAsync(entity, ct);

    public void Remove(Encounter entity) => db.Encounters.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    // Deliberately not ordering Combatants here — same reasoning as
    // EfCampaignRepository.GetWithCharactersAsync: sort by OrderIndex client-side instead.
    public async Task<Encounter?> GetWithCombatantsAsync(Guid id, CancellationToken ct = default) =>
        await db.Encounters
            .Include(e => e.Combatants)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
}
