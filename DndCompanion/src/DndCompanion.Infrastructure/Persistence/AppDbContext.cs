using System.Text.Json;
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
    }
}
