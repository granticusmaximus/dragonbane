namespace DndCompanion.Domain.Entities;

public class Encounter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionLogId { get; set; }
    public SessionLog SessionLog { get; set; } = null!;
    public string Name { get; set; } = "";
    public EncounterStatus Status { get; set; } = EncounterStatus.Planned;
    public int CurrentRound { get; set; }
    public int CurrentTurnIndex { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<Combatant> Combatants { get; set; } = [];
}

/// <summary>
/// One PC or freeform NPC/monster in an encounter's initiative order. <see cref="OrderIndex"/>
/// (not a query-time sort on <see cref="InitiativeRoll"/>) is the authoritative turn order —
/// set from initiative descending when combatants are added, then freely reorderable for ties
/// — this sidesteps the ordered-Include staleness gotcha (see CLAUDE.md) by making order an
/// explicit, always-client-sorted column instead.
/// </summary>
public class Combatant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EncounterId { get; set; }
    public Encounter Encounter { get; set; } = null!;
    /// <summary>Set when this combatant is a linked PC; null for freeform NPCs/monsters.</summary>
    public Guid? CharacterId { get; set; }
    /// <summary>Provenance only (Phase 8 bestiary) — a snapshot pointer, never re-read live.</summary>
    public Guid? MonsterTemplateId { get; set; }
    public string Name { get; set; } = "";
    public int InitiativeRoll { get; set; }
    public int OrderIndex { get; set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int ArmorClass { get; set; }
    public string? ConditionsCsv { get; set; }
    public bool IsDefeated { get; set; }
}
