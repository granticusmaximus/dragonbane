namespace DndCompanion.Domain.Entities;

/// <summary>A named group of a character's saved dice sets (e.g. "Sneak Attack Round").</summary>
public class DiceSetFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }

    public List<DiceSet> DiceSets { get; set; } = [];
}

/// <summary>A saved, reusable dice expression (e.g. "Fire Bolt" = "2d10"), one tap to re-roll.</summary>
public class DiceSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FolderId { get; set; }
    public DiceSetFolder Folder { get; set; } = null!;
    public string Name { get; set; } = "";
    /// <summary>Validated via DiceExpression.TryParse at save time — never persisted unparseable.</summary>
    public string Expression { get; set; } = "";
    public int SortOrder { get; set; }
}
