namespace DndCompanion.Domain.ValueObjects;

/// <summary>Six ability scores. Modifier is derived, never stored.</summary>
public sealed record AbilityScores(
    int Strength, int Dexterity, int Constitution,
    int Intelligence, int Wisdom, int Charisma)
{
    public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2.0);

    public int StrMod => Modifier(Strength);
    public int DexMod => Modifier(Dexterity);
    public int ConMod => Modifier(Constitution);
    public int IntMod => Modifier(Intelligence);
    public int WisMod => Modifier(Wisdom);
    public int ChaMod => Modifier(Charisma);
}
