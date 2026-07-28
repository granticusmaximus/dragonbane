using System.Text.Json;
using DndCompanion.Domain;
using DndCompanion.Domain.Entities;
using DndCompanion.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<ContentSource> ContentSources => Set<ContentSource>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Spell> Spells => Set<Spell>();
    public DbSet<ActionDef> ActionDefs => Set<ActionDef>();
    public DbSet<SessionLog> SessionLogs => Set<SessionLog>();
    public DbSet<ActionEntry> ActionEntries => Set<ActionEntry>();
    public DbSet<Recording> Recordings => Set<Recording>();
    public DbSet<TranscriptSegment> TranscriptSegments => Set<TranscriptSegment>();
    public DbSet<StructuredNote> StructuredNotes => Set<StructuredNote>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Combatant> Combatants => Set<Combatant>();
    public DbSet<MonsterTemplate> MonsterTemplates => Set<MonsterTemplate>();
    public DbSet<DiceSetFolder> DiceSetFolders => Set<DiceSetFolder>();
    public DbSet<DiceSet> DiceSets => Set<DiceSet>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Composite key for the campaign<->character join.
        b.Entity<CampaignCharacter>().HasKey(cc => new { cc.CampaignId, cc.CharacterId });

        // Store the AbilityScores value object as a single JSON column.
        b.Entity<Character>().Property(c => c.Abilities)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<AbilityScores>(v, (JsonSerializerOptions?)null)!)
            .HasColumnType("TEXT");

        // Same JSON-column treatment for spell slots.
        b.Entity<Character>().Property(c => c.SpellSlots)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<SpellSlots>(v, (JsonSerializerOptions?)null)!)
            .HasColumnType("TEXT");

        // Useful lookups for the rules/items browser.
        b.Entity<Spell>().HasIndex(s => new { s.Level, s.Name });
        b.Entity<Item>().HasIndex(i => new { i.Category, i.Name });
        b.Entity<ActionEntry>().HasIndex(e => new { e.SessionLogId, e.CreatedUtc });

        // Enums as text so the DB stays human-readable.
        b.Entity<ContentSource>().Property(c => c.Kind).HasConversion<string>();
        b.Entity<Spell>().Property(s => s.School).HasConversion<string>();
        b.Entity<ActionDef>().Property(a => a.ActionType).HasConversion<string>();
        b.Entity<ActionEntry>().Property(e => e.Source).HasConversion<string>();
        b.Entity<StructuredNote>().Property(n => n.Kind).HasConversion<string>();
        b.Entity<Character>().Property(c => c.Size).HasConversion<string>();
        b.Entity<Character>().Property(c => c.ProficientSkills).HasConversion<string>();
        b.Entity<Character>().Property(c => c.ExpertSkills).HasConversion<string>();
        b.Entity<Character>().Property(c => c.ProficientSaves).HasConversion<string>();
        b.Entity<Encounter>().Property(e => e.Status).HasConversion<string>();
        b.Entity<Combatant>().HasIndex(c => new { c.EncounterId, c.OrderIndex });

        // Match the C# property initializers so existing rows backfill sensibly on migration.
        // EF's migration-default inference doesn't follow HasConversion (confirmed empirically:
        // generating without these lines produced defaultValue: "" for every converted property
        // below, which then throws on read — Enum.Parse("") and JSON-deserializing "" both fail)
        // — every converted/non-zero-default property needs an explicit HasDefaultValue.
        b.Entity<Character>().Property(c => c.ArmorClass).HasDefaultValue(10);
        b.Entity<Character>().Property(c => c.Speed).HasDefaultValue(30);
        b.Entity<Character>().Property(c => c.Size).HasDefaultValue(SizeCategory.Medium);
        b.Entity<Character>().Property(c => c.ProficientSkills).HasDefaultValue(Skill.None);
        b.Entity<Character>().Property(c => c.ExpertSkills).HasDefaultValue(Skill.None);
        b.Entity<Character>().Property(c => c.ProficientSaves).HasDefaultValue(Ability.None);
        b.Entity<Character>().Property(c => c.SpellSlots).HasDefaultValue(SpellSlots.Empty);
    }
}
