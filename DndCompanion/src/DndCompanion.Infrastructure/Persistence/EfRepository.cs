using DndCompanion.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Persistence;

/// <summary>Generic EF Core-backed IRepository&lt;T&gt; — one implementation shared across
/// every entity so UI/Application never need to reference EF Core or AppDbContext directly.</summary>
public sealed class EfRepository<T>(AppDbContext db) : IRepository<T> where T : class
{
    public async Task<T?> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Set<T>().FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default) =>
        await db.Set<T>().AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await db.Set<T>().AddAsync(entity, ct);

    public void Remove(T entity) => db.Set<T>().Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
