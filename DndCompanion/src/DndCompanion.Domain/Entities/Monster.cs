namespace DndCompanion.Domain.Entities;

/// <summary>
/// A reusable, DM-authored monster/NPC stat line for the Bestiary. Deliberately no
/// <see cref="ContentSource"/>/SRD tagging — unlike Item/Spell/ActionDef, this is always
/// freeform DM content by design (no SRD monster compendium is imported), so the
/// SRD/Homebrew provenance system that exists for licensing separation doesn't apply here.
/// </summary>
public class MonsterTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int HpMax { get; set; }
    public int ArmorClass { get; set; }
    public int InitiativeBonus { get; set; }
    public string? StatBlockText { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
