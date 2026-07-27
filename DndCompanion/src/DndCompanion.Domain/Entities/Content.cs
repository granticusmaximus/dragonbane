namespace DndCompanion.Domain.Entities;

/// <summary>Provenance for every rules row — keeps SRD vs PHB vs Homebrew separable.</summary>
public class ContentSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ContentKind Kind { get; set; }
    public string Attribution { get; set; } = "";  // required CC-BY string for SRD rows
}

public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public ContentSource Source { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";     // weapon, armor, gear, tool...
    public double? WeightLb { get; set; }
    public string? Cost { get; set; }
    public string? PropertiesJson { get; set; }    // finesse, thrown, range, etc.
    public string? RulesText { get; set; }
}

public class CharacterItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
    public int Quantity { get; set; } = 1;
    public bool Equipped { get; set; }
    public bool Attuned { get; set; }
}

public class Spell
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public ContentSource Source { get; set; } = null!;
    public string Name { get; set; } = "";
    public int Level { get; set; }                 // 0 = cantrip
    public SpellSchool School { get; set; }
    public string CastingTime { get; set; } = "";
    public string Range { get; set; } = "";
    public string Components { get; set; } = "";
    public string Duration { get; set; } = "";
    public bool Concentration { get; set; }
    public string? RulesText { get; set; }
    public string? ScalingJson { get; set; }       // higher-level / cantrip scaling
}

public class CharacterSpell
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public Guid SpellId { get; set; }
    public Spell Spell { get; set; } = null!;
    public bool IsPrepared { get; set; }
    public bool AlwaysPrepared { get; set; }       // e.g. Druidic -> Speak with Animals
}

public class ActionDef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public ContentSource Source { get; set; } = null!;
    public string Name { get; set; } = "";
    public ActionType ActionType { get; set; }
    public string? RulesText { get; set; }
}

public class CharacterAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public Guid ActionDefId { get; set; }
    public ActionDef ActionDef { get; set; } = null!;
    public string? Notes { get; set; }
}
