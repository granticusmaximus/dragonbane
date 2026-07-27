using DndCompanion.Application.Dice;
using DndCompanion.Domain.ValueObjects;

namespace DndCompanion.Application.Abstractions;

public enum RollMode { Normal, Advantage, Disadvantage }

public interface IDiceRoller
{
    /// <summary>Roll a parsed expression, e.g. Cure Wounds = 2d8+2.</summary>
    DiceResult Roll(DiceExpression expr, string? label = null);

    /// <summary>d20 attack/check with optional advantage/disadvantage and a flat modifier.</summary>
    DiceResult RollD20(int modifier, RollMode mode = RollMode.Normal, string? label = null);
}
