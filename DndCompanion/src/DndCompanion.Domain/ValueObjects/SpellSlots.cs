namespace DndCompanion.Domain.ValueObjects;

/// <summary>
/// Explicit numbered properties rather than an array — records only get free structural
/// equality on scalar properties, not array/list-typed ones.
/// </summary>
public sealed record SpellSlots(
    int Level1Current, int Level1Max, int Level2Current, int Level2Max,
    int Level3Current, int Level3Max, int Level4Current, int Level4Max,
    int Level5Current, int Level5Max, int Level6Current, int Level6Max,
    int Level7Current, int Level7Max, int Level8Current, int Level8Max,
    int Level9Current, int Level9Max)
{
    public static SpellSlots Empty => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public int CurrentAt(int level) => level switch
    {
        1 => Level1Current, 2 => Level2Current, 3 => Level3Current, 4 => Level4Current, 5 => Level5Current,
        6 => Level6Current, 7 => Level7Current, 8 => Level8Current, 9 => Level9Current, _ => 0
    };

    public int MaxAt(int level) => level switch
    {
        1 => Level1Max, 2 => Level2Max, 3 => Level3Max, 4 => Level4Max, 5 => Level5Max,
        6 => Level6Max, 7 => Level7Max, 8 => Level8Max, 9 => Level9Max, _ => 0
    };

    public SpellSlots WithCurrentAt(int level, int value) => level switch
    {
        1 => this with { Level1Current = value }, 2 => this with { Level2Current = value },
        3 => this with { Level3Current = value }, 4 => this with { Level4Current = value },
        5 => this with { Level5Current = value }, 6 => this with { Level6Current = value },
        7 => this with { Level7Current = value }, 8 => this with { Level8Current = value },
        9 => this with { Level9Current = value }, _ => this
    };

    public SpellSlots WithMaxAt(int level, int value) => level switch
    {
        1 => this with { Level1Max = value }, 2 => this with { Level2Max = value },
        3 => this with { Level3Max = value }, 4 => this with { Level4Max = value },
        5 => this with { Level5Max = value }, 6 => this with { Level6Max = value },
        7 => this with { Level7Max = value }, 8 => this with { Level8Max = value },
        9 => this with { Level9Max = value }, _ => this
    };
}
