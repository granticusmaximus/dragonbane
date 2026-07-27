namespace DndCompanion.Infrastructure.Srd;

/// <summary>Shape of a row in /data/spells.json — decoupled from the Spell entity so the
/// JSON format can evolve without touching EF mapping.</summary>
public sealed record SpellImportDto(
    string Name,
    int Level,
    string School,
    string CastingTime,
    string Range,
    string Components,
    string Duration,
    bool Concentration,
    string? RulesText,
    string? ScalingJson);

/// <summary>Shape of a row in /data/items.json.</summary>
public sealed record ItemImportDto(
    string Name,
    string Category,
    double? WeightLb,
    string? Cost,
    string? PropertiesJson,
    string? RulesText);

/// <summary>Shape of a row in /data/actions.json.</summary>
public sealed record ActionImportDto(
    string Name,
    string ActionType,
    string? RulesText);
