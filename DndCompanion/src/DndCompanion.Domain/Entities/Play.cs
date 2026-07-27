namespace DndCompanion.Domain.Entities;

public class SessionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;
    public DateTime SessionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public List<ActionEntry> Entries { get; set; } = [];
    public List<Recording> Recordings { get; set; } = [];
}

/// <summary>One logged thing that happened — powers "previous actions" and the timeline.</summary>
public class ActionEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionLogId { get; set; }
    public SessionLog SessionLog { get; set; } = null!;
    public Guid? CharacterId { get; set; }
    public int? RoundNo { get; set; }
    public int? InitiativeSlot { get; set; }
    public string Description { get; set; } = "";
    public string? DiceResultJson { get; set; }    // serialized DiceResult
    public EntrySource Source { get; set; } = EntrySource.Manual;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
