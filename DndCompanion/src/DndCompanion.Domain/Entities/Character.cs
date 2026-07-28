using DndCompanion.Domain;
using DndCompanion.Domain.ValueObjects;

namespace DndCompanion.Domain.Entities;

public class Character
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Species { get; set; } = "";      // e.g. "Aasimar" (may be a homebrew row)
    public string Class { get; set; } = "";
    public string? Subclass { get; set; }
    public int Level { get; set; } = 1;
    public string? Background { get; set; }
    public int ProficiencyBonus { get; set; } = 2;

    /// <summary>Stored as a JSON column; see AppDbContext configuration.</summary>
    public AbilityScores Abilities { get; set; } = new(10, 10, 10, 10, 10, 10);
    public string? Notes { get; set; }

    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int TempHp { get; set; }
    public int ArmorClass { get; set; } = 10;
    public int Speed { get; set; } = 30;
    /// <summary>Bonus BEYOND DexMod — total initiative modifier is DexMod + this, computed at roll time.</summary>
    public int InitiativeBonus { get; set; }
    public SizeCategory Size { get; set; } = SizeCategory.Medium;
    public string? Alignment { get; set; }
    public Skill ProficientSkills { get; set; }
    public Skill ExpertSkills { get; set; }
    public Ability ProficientSaves { get; set; }
    /// <summary>Stored as a JSON column; see AppDbContext configuration.</summary>
    public SpellSlots SpellSlots { get; set; } = SpellSlots.Empty;

    public List<CampaignCharacter> Campaigns { get; set; } = [];
    public List<CharacterItem> Items { get; set; } = [];
    public List<CharacterSpell> Spells { get; set; } = [];
    public List<CharacterAction> Actions { get; set; } = [];
}

/// <summary>Join: a character can be used across many campaigns.</summary>
public class CampaignCharacter
{
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
}
