using System.Text.Json;
using DndCompanion.Domain;
using DndCompanion.Domain.Entities;
using DndCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Srd;

/// <summary>
/// Seeds the DB from SRD 5.2 (CC-BY-4.0) content dropped as JSON in /data. Every seeded
/// row carries the SRD ContentSource so it stays legally separable from PHB/homebrew rows.
///
/// NOTE: SRD 5.2 excludes Aasimar, Artificer, some monsters — those are entered as
/// Homebrew rows by the user, never bundled/redistributed.
/// </summary>
public sealed class SrdImporter(AppDbContext db)
{
    public const string SrdAttribution =
        "This work includes material from the System Reference Document 5.2 " +
        "(\"SRD 5.2\") by Wizards of the Coast LLC, available under the Creative " +
        "Commons Attribution 4.0 International License.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<int> ImportAsync(string dataDir, CancellationToken ct = default)
    {
        // Import is a one-time seed — re-running (e.g. on every app start) is a no-op
        // once SRD content exists, so it's safe to call unconditionally at startup.
        if (await db.ContentSources.AnyAsync(c => c.Kind == ContentKind.Srd, ct))
            return 0;

        var srd = new ContentSource { Kind = ContentKind.Srd, Attribution = SrdAttribution };
        db.ContentSources.Add(srd);

        await ImportSpellsAsync(db, srd, dataDir, ct);
        await ImportItemsAsync(db, srd, dataDir, ct);
        await ImportActionsAsync(db, srd, dataDir, ct);

        return await db.SaveChangesAsync(ct);
    }

    private static async Task ImportSpellsAsync(AppDbContext db, ContentSource srd, string dataDir, CancellationToken ct)
    {
        var dtos = await ReadAsync<SpellImportDto>(dataDir, "spells.json", ct);
        foreach (var d in dtos)
        {
            if (!Enum.TryParse<SpellSchool>(d.School, ignoreCase: true, out var school))
                throw new InvalidDataException($"Unknown spell school '{d.School}' for spell '{d.Name}'.");

            db.Spells.Add(new Spell
            {
                Source = srd,
                Name = d.Name,
                Level = d.Level,
                School = school,
                CastingTime = d.CastingTime,
                Range = d.Range,
                Components = d.Components,
                Duration = d.Duration,
                Concentration = d.Concentration,
                RulesText = d.RulesText,
                ScalingJson = d.ScalingJson
            });
        }
    }

    private static async Task ImportItemsAsync(AppDbContext db, ContentSource srd, string dataDir, CancellationToken ct)
    {
        var dtos = await ReadAsync<ItemImportDto>(dataDir, "items.json", ct);
        foreach (var d in dtos)
        {
            db.Items.Add(new Item
            {
                Source = srd,
                Name = d.Name,
                Category = d.Category,
                WeightLb = d.WeightLb,
                Cost = d.Cost,
                PropertiesJson = d.PropertiesJson,
                RulesText = d.RulesText
            });
        }
    }

    private static async Task ImportActionsAsync(AppDbContext db, ContentSource srd, string dataDir, CancellationToken ct)
    {
        var dtos = await ReadAsync<ActionImportDto>(dataDir, "actions.json", ct);
        foreach (var d in dtos)
        {
            if (!Enum.TryParse<ActionType>(d.ActionType, ignoreCase: true, out var actionType))
                throw new InvalidDataException($"Unknown action type '{d.ActionType}' for action '{d.Name}'.");

            db.ActionDefs.Add(new ActionDef
            {
                Source = srd,
                Name = d.Name,
                ActionType = actionType,
                RulesText = d.RulesText
            });
        }
    }

    private static async Task<List<T>> ReadAsync<T>(string dataDir, string fileName, CancellationToken ct)
    {
        var path = Path.Combine(dataDir, fileName);
        if (!File.Exists(path))
            return [];

        await using var stream = File.OpenRead(path);
        var dtos = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct);
        return dtos ?? [];
    }
}
