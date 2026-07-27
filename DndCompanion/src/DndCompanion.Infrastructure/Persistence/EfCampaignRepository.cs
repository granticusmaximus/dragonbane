using DndCompanion.Application.Abstractions;
using DndCompanion.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Persistence;

public sealed class EfCampaignRepository(AppDbContext db) : ICampaignRepository
{
    public async Task<Campaign?> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Campaigns.FindAsync([id], ct);

    public async Task<IReadOnlyList<Campaign>> ListAsync(CancellationToken ct = default) =>
        await db.Campaigns.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public async Task AddAsync(Campaign entity, CancellationToken ct = default) =>
        await db.Campaigns.AddAsync(entity, ct);

    public void Remove(Campaign entity) => db.Campaigns.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Campaign>> ListActiveAsync(CancellationToken ct = default) =>
        await db.Campaigns.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Campaign?> GetWithCharactersAsync(Guid id, CancellationToken ct = default) =>
        await db.Campaigns
            .Include(c => c.Characters).ThenInclude(cc => cc.Character)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
