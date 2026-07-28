using DndCompanion.Domain.Entities;

namespace DndCompanion.Application.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface ICampaignRepository : IRepository<Campaign>
{
    Task<IReadOnlyList<Campaign>> ListActiveAsync(CancellationToken ct = default);
    Task<Campaign?> GetWithCharactersAsync(Guid id, CancellationToken ct = default);
}

public interface ICharacterRepository : IRepository<Character>
{
    Task<Character?> GetSheetAsync(Guid id, CancellationToken ct = default); // items+spells+actions
}

public interface IEncounterRepository : IRepository<Encounter>
{
    Task<Encounter?> GetWithCombatantsAsync(Guid id, CancellationToken ct = default);
}
