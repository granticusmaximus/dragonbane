namespace DndCompanion.Domain.ValueObjects;

/// <summary>A single rolled die.</summary>
public sealed record Die(int Sides, int Value);

/// <summary>The outcome of evaluating a dice expression (e.g. "2d8+2").</summary>
public sealed record DiceResult(string Label, IReadOnlyList<Die> Dice, int Modifier)
{
    public int DiceTotal => Dice.Sum(d => d.Value);
    public int Total => DiceTotal + Modifier;
    public bool IsNat20 => Dice.Count == 1 && Dice[0].Sides == 20 && Dice[0].Value == 20;
    public bool IsNat1  => Dice.Count == 1 && Dice[0].Sides == 20 && Dice[0].Value == 1;
}
