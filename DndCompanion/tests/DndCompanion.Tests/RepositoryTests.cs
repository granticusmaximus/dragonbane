using DndCompanion.Domain;
using DndCompanion.Domain.Entities;
using DndCompanion.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Tests;

public class RepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public RepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Campaign_repository_lists_only_active_campaigns()
    {
        var repo = new EfCampaignRepository(_db);
        await repo.AddAsync(new Campaign { Name = "Active One", IsActive = true });
        await repo.AddAsync(new Campaign { Name = "Retired", IsActive = false });
        await repo.SaveChangesAsync();

        var active = await repo.ListActiveAsync();

        Assert.Single(active);
        Assert.Equal("Active One", active[0].Name);
    }

    [Fact]
    public async Task Campaign_repository_loads_characters_with_GetWithCharactersAsync()
    {
        var campaignRepo = new EfCampaignRepository(_db);
        var characterRepo = new EfCharacterRepository(_db);

        var character = new Character { Name = "Lyra", Species = "Human", Class = "Cleric" };
        await characterRepo.AddAsync(character);

        var campaign = new Campaign { Name = "Curse of Strahd" };
        await campaignRepo.AddAsync(campaign);
        await campaignRepo.SaveChangesAsync();

        campaign.Characters.Add(new CampaignCharacter { CampaignId = campaign.Id, CharacterId = character.Id });
        await campaignRepo.SaveChangesAsync();

        var loaded = await campaignRepo.GetWithCharactersAsync(campaign.Id);

        Assert.NotNull(loaded);
        var cc = Assert.Single(loaded!.Characters);
        Assert.Equal("Lyra", cc.Character.Name);
    }

    [Fact]
    public async Task Character_repository_GetSheetAsync_includes_homebrew_item_with_source()
    {
        var characterRepo = new EfCharacterRepository(_db);
        var itemRepo = new EfRepository<Item>(_db);
        var sourceRepo = new EfRepository<ContentSource>(_db);

        var homebrew = new ContentSource { Kind = ContentKind.Homebrew, Attribution = "User-created homebrew content." };
        await sourceRepo.AddAsync(homebrew);
        await sourceRepo.SaveChangesAsync();

        var item = new Item { Source = homebrew, Name = "Blade of the Ancients", Category = "Weapon" };
        await itemRepo.AddAsync(item);
        await itemRepo.SaveChangesAsync();

        var character = new Character { Name = "Kaelen", Species = "Aasimar", Class = "Paladin" };
        await characterRepo.AddAsync(character);
        await characterRepo.SaveChangesAsync();

        var characterItemRepo = new EfRepository<CharacterItem>(_db);
        await characterItemRepo.AddAsync(new CharacterItem { CharacterId = character.Id, ItemId = item.Id, Quantity = 1 });
        await characterItemRepo.SaveChangesAsync();

        var sheet = await characterRepo.GetSheetAsync(character.Id);

        Assert.NotNull(sheet);
        var ci = Assert.Single(sheet!.Items);
        Assert.Equal("Blade of the Ancients", ci.Item.Name);
        Assert.Equal(ContentKind.Homebrew, ci.Item.Source.Kind);
    }

    [Fact]
    public async Task Generic_repository_supports_add_get_list_remove()
    {
        var repo = new EfRepository<ContentSource>(_db);
        var source = new ContentSource { Kind = ContentKind.Homebrew, Attribution = "Test" };

        await repo.AddAsync(source);
        await repo.SaveChangesAsync();

        var fetched = await repo.GetAsync(source.Id);
        Assert.NotNull(fetched);
        Assert.Single(await repo.ListAsync());

        repo.Remove(fetched!);
        await repo.SaveChangesAsync();

        Assert.Empty(await repo.ListAsync());
    }
}
