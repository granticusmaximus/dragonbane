using DndCompanion.Application.Abstractions;
using DndCompanion.Application.Dice;
using DndCompanion.Domain.ValueObjects;

namespace DndCompanion.Infrastructure.Dice;

/// <summary>
/// Default roller. Uses Random.Shared; swap for RandomNumberGenerator if you ever
/// want cryptographic fairness. Kept behind IDiceRoller so tests can inject a seed.
/// </summary>
public sealed class DiceRoller : IDiceRoller
{
    private readonly Random _rng;
    public DiceRoller(Random? rng = null) => _rng = rng ?? Random.Shared;

    private int RollDie(int sides) => _rng.Next(1, sides + 1);

    public DiceResult Roll(DiceExpression expr, string? label = null)
    {
        var dice = new List<Die>(expr.Count);
        for (var i = 0; i < expr.Count; i++)
            dice.Add(new Die(expr.Sides, RollDie(expr.Sides)));
        return new DiceResult(label ?? expr.ToString(), dice, expr.Modifier);
    }

    public DiceResult RollD20(int modifier, RollMode mode = RollMode.Normal, string? label = null)
    {
        var a = RollDie(20);
        if (mode == RollMode.Normal)
            return new DiceResult(label ?? $"d20{Sign(modifier)}", [new Die(20, a)], modifier);

        var b = RollDie(20);
        var kept = mode == RollMode.Advantage ? Math.Max(a, b) : Math.Min(a, b);
        var suffix = mode == RollMode.Advantage ? " (adv)" : " (dis)";
        return new DiceResult((label ?? "d20") + suffix, [new Die(20, kept)], modifier);
    }

    private static string Sign(int m) => m == 0 ? "" : m > 0 ? $"+{m}" : m.ToString();
}
