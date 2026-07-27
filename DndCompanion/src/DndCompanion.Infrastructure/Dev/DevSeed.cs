using DndCompanion.Domain;
using DndCompanion.Domain.Entities;
using DndCompanion.Domain.ValueObjects;
using DndCompanion.Infrastructure.Persistence;
using DndCompanion.Infrastructure.Srd;
using Microsoft.EntityFrameworkCore;

namespace DndCompanion.Infrastructure.Dev;

/// <summary>
/// Dev-only fixture: seeds one hand-authored character ("Baloney Slim") for exercising the
/// character sheet UI against realistic data. Wired from Program.cs behind an
/// IsDevelopment() check — never runs in Production/packaged builds. Idempotent: re-running
/// after the character exists is a no-op.
///
/// Several character-sheet facts are still unconfirmed by the player — see the TODO markers
/// below and in the seeded Character.Notes. Do not treat this data as final until those are
/// resolved.
/// </summary>
public static class DevSeed
{
    private const string HomebrewAttribution = "User-created homebrew content.";
    private const string BackfillTodo = "TODO: backfill real SRD/PHB text — this row was created by DevSeed as a placeholder.";

    public static async Task BaloneySlimAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Characters.AnyAsync(c => c.Name == "Baloney Slim", ct))
            return;

        var srd = await GetOrCreateSourceAsync(db, ContentKind.Srd, SrdImporter.SrdAttribution, ct);
        var homebrew = await GetOrCreateSourceAsync(db, ContentKind.Homebrew, HomebrewAttribution, ct);

        var character = new Character
        {
            Name = "Baloney Slim",
            Species = "Aasimar", // not in SRD 5.2 — homebrew species entry, see CLAUDE.md
            Class = "Druid",
            Subclass = null, // Circle chosen at level 3
            Level = 1,
            Background = "Sage",
            ProficiencyBonus = 2,
            Abilities = new AbilityScores(
                Strength: 14, Dexterity: 14, Constitution: 13,
                Intelligence: 15, Wisdom: 15, Charisma: 13),
            Notes =
                """
                Celestial name: Seraviel Bratwurstson

                Proficiencies (tracked here until a real proficiency model exists):
                  Saves: Intelligence, Wisdom
                  Skills: Arcana, History, Perception, Animal Handling
                  Tool: Calligrapher's Supplies

                Species traits (Aasimar — homebrew, not in SRD 5.2):
                  Darkvision 60 ft
                  Celestial Resistance (radiant/necrotic)
                  Healing Hands (2d4)
                  Light Bearer

                Feature: Magic Initiate (Wizard) — cantrips Fire Bolt + Minor Illusion, 1st-level spell Shield
                  (cast once per long rest without a slot; seeded as always-prepared, doesn't count against
                  the 4 prepared 1st-level slots below).

                TODO: "Druidic focus" equipment reused the existing SRD "Druidic Focus" item rather than
                  creating a separate "Sprig of Mistletoe" row — same mechanical item, different flavor text.

                TODO: Thunderclap (Druid cantrip, replacing Starry Wisp) isn't in the SRD seed data — seeded
                  as a Homebrew placeholder pending backfill; it's real SRD content, just not yet imported.
                """
        };
        db.Add(character);
        await db.SaveChangesAsync(ct);

        // ---- Cantrips ----
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Druidcraft", srd, 0, SpellSchool.Transmutation, ct), isPrepared: true);
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Produce Flame", srd, 0, SpellSchool.Conjuration, ct), isPrepared: true);
        // Thunderclap replaces Starry Wisp (confirmed) — not in the SRD seed data, minimal Homebrew
        // placeholder with a backfill TODO; it's real SRD content, just never imported.
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Thunderclap", homebrew, 0, SpellSchool.Evocation, ct), isPrepared: true);
        // Light is granted via the Aasimar species trait (homebrew in this app), not the Druid spell list.
        // Per the seeding brief this is tagged/created against the Homebrew source specifically — scoped to
        // Homebrew so it does NOT reuse the real SRD "Light" cantrip row, which stays correctly SRD-tagged
        // for every other lookup. This intentionally leaves two "Light" spell rows in the DB (one SRD, one
        // Homebrew) — see the report for this tradeoff; a cleaner fix would track *how* a character learned
        // a spell separately from the spell's own ContentSource, but that's a schema change, out of scope here.
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Light", homebrew, 0, SpellSchool.Evocation, ct, scopeToSource: true), isPrepared: true);
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Fire Bolt", srd, 0, SpellSchool.Evocation, ct), isPrepared: true);
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Minor Illusion", srd, 0, SpellSchool.Illusion, ct), isPrepared: true);

        // ---- 1st-level prepared ----
        // Faerie Fire is a gap in the SRD seed data (not homebrew content, just never imported) — minimal
        // row, tagged Homebrew with a backfill TODO per the seeding brief's rule for anything not yet seeded.
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Faerie Fire", homebrew, 1, SpellSchool.Evocation, ct), isPrepared: true);
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Healing Word", srd, 1, SpellSchool.Abjuration, ct), isPrepared: true);
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Cure Wounds", srd, 1, SpellSchool.Abjuration, ct), isPrepared: true);
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Entangle", srd, 1, SpellSchool.Conjuration, ct), isPrepared: true);

        // ---- Always-prepared (free — do not count against the 4 prepared 1st-level slots above) ----
        // Druidic feature.
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Speak with Animals", srd, 1, SpellSchool.Divination, ct), isPrepared: true, alwaysPrepared: true);
        // Magic Initiate (Wizard) 1st-level spell — castable once per long rest without expending a slot.
        LinkSpell(db, character, await FindOrCreateSpellAsync(db, "Shield", srd, 1, SpellSchool.Abjuration, ct), isPrepared: true, alwaysPrepared: true);

        // ---- Equipment ----
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Leather Armor", srd, "Armor", ct), equipped: true);
        // "Shield" (the AC item) is missing from the SRD seed data, same kind of gap as Faerie Fire above.
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Wooden Shield", homebrew, "Armor", ct), equipped: true);
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Quarterstaff", srd, "Weapon", ct), equipped: true);
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Dagger", srd, "Weapon", ct), equipped: false);
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Druidic Focus", srd, "Adventuring Gear", ct), equipped: true);
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Explorer's Pack", srd, "Adventuring Gear", ct), equipped: false);
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Calligrapher's Supplies", srd, "Tool", ct), equipped: false);
        LinkItem(db, character, await FindOrCreateItemAsync(db, "Scholar's Journal", homebrew, "Adventuring Gear", ct), equipped: false);

        await db.SaveChangesAsync(ct);
    }

    private static async Task<ContentSource> GetOrCreateSourceAsync(
        AppDbContext db, ContentKind kind, string attribution, CancellationToken ct)
    {
        var existing = await db.ContentSources.FirstOrDefaultAsync(s => s.Kind == kind, ct);
        if (existing is not null) return existing;

        var source = new ContentSource { Kind = kind, Attribution = attribution };
        db.Add(source);
        await db.SaveChangesAsync(ct);
        return source;
    }

    /// <summary>Find a Spell by name (optionally scoped to a specific source), or create a minimal
    /// placeholder row tagged with <paramref name="sourceForCreate"/> if none exists.</summary>
    private static async Task<Spell> FindOrCreateSpellAsync(
        AppDbContext db, string name, ContentSource sourceForCreate, int level, SpellSchool school,
        CancellationToken ct, bool scopeToSource = false)
    {
        var query = db.Spells.Where(s => s.Name == name);
        if (scopeToSource) query = query.Where(s => s.SourceId == sourceForCreate.Id);
        var existing = await query.FirstOrDefaultAsync(ct);
        if (existing is not null) return existing;

        var spell = new Spell
        {
            Source = sourceForCreate,
            Name = name,
            Level = level,
            School = school,
            CastingTime = "TODO",
            Range = "TODO",
            Components = "TODO",
            Duration = "TODO",
            Concentration = false,
            RulesText = BackfillTodo
        };
        db.Add(spell);
        await db.SaveChangesAsync(ct);
        return spell;
    }

    private static async Task<Item> FindOrCreateItemAsync(
        AppDbContext db, string name, ContentSource sourceForCreate, string category, CancellationToken ct)
    {
        var existing = await db.Items.FirstOrDefaultAsync(i => i.Name == name, ct);
        if (existing is not null) return existing;

        var item = new Item
        {
            Source = sourceForCreate,
            Name = name,
            Category = category,
            RulesText = BackfillTodo
        };
        db.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    private static void LinkSpell(AppDbContext db, Character character, Spell spell, bool isPrepared, bool alwaysPrepared = false) =>
        db.Add(new CharacterSpell
        {
            CharacterId = character.Id,
            SpellId = spell.Id,
            IsPrepared = isPrepared,
            AlwaysPrepared = alwaysPrepared
        });

    private static void LinkItem(AppDbContext db, Character character, Item item, bool equipped) =>
        db.Add(new CharacterItem
        {
            CharacterId = character.Id,
            ItemId = item.Id,
            Quantity = 1,
            Equipped = equipped
        });
}
