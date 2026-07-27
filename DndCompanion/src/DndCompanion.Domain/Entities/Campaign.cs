namespace DndCompanion.Domain.Entities;

public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<CampaignCharacter> Characters { get; set; } = [];
    public List<SessionLog> Sessions { get; set; } = [];
}
