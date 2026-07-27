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

    // Deliberately not ordering Sessions in the query itself: within a single Blazor Server
    // circuit, AppDbContext is scoped to the whole circuit, so this can run against an already-
    // tracked Campaign whose Sessions collection was populated by an earlier AddAsync in the same
    // circuit. EF Core's navigation fixup doesn't re-sort an already-populated tracked collection
    // to match a fresh query's ORDER BY, so an ordered Include here would silently stop reflecting
    // reality after the first add. Callers sort client-side instead (see CampaignDetailPage).
    public async Task<Campaign?> GetWithCharactersAsync(Guid id, CancellationToken ct = default) =>
        await db.Campaigns
            .Include(c => c.Characters).ThenInclude(cc => cc.Character)
            .Include(c => c.Sessions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
}
