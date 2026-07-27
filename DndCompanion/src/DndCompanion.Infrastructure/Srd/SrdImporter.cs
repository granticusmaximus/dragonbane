using DndCompanion.Domain;
using DndCompanion.Domain.Entities;
using DndCompanion.Infrastructure.Persistence;

namespace DndCompanion.Infrastructure.Srd;

/// <summary>
/// Seeds the DB from SRD 5.2 (CC-BY-4.0) content. STUB — fill in a parser for the
/// SRD data you drop into /data (JSON is easiest). Every seeded row must carry the
/// SRD ContentSource so it stays legally separable from PHB/homebrew rows.
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

    public async Task<int> ImportAsync(string dataDir, CancellationToken ct = default)
    {
        var srd = new ContentSource { Kind = ContentKind.Srd, Attribution = SrdAttribution };
        db.ContentSources.Add(srd);

        // TODO: parse spells/items/actions from `dataDir` and attach `srd`.
        //   foreach (var s in ParseSpells(dataDir)) { s.Source = srd; db.Spells.Add(s); }
        //   foreach (var i in ParseItems(dataDir))  { i.Source = srd; db.Items.Add(i); }

        return await db.SaveChangesAsync(ct);
    }
}
