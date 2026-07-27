using DndCompanion.Application.Abstractions;
using DndCompanion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Persistence;

public sealed class EfCharacterRepository(AppDbContext db) : ICharacterRepository
{
    public async Task<Character?> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Characters.FindAsync([id], ct);

    public async Task<IReadOnlyList<Character>> ListAsync(CancellationToken ct = default) =>
        await db.Characters.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Character entity, CancellationToken ct = default) =>
        await db.Characters.AddAsync(entity, ct);

    public void Remove(Character entity) => db.Characters.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    public async Task<Character?> GetSheetAsync(Guid id, CancellationToken ct = default) =>
        await db.Characters
            .Include(c => c.Items).ThenInclude(ci => ci.Item).ThenInclude(i => i.Source)
            .Include(c => c.Spells).ThenInclude(cs => cs.Spell).ThenInclude(s => s.Source)
            .Include(c => c.Actions).ThenInclude(ca => ca.ActionDef).ThenInclude(a => a.Source)
            .Include(c => c.Campaigns).ThenInclude(cc => cc.Campaign).ThenInclude(camp => camp.Sessions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
