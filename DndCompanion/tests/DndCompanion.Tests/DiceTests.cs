using DndCompanion.Application.Dice;
using DndCompanion.Infrastructure.Dice;

namespace DndCompanion.Tests;

public class DiceTests
{
    [Theory]
    [InlineData("2d8+2", 2, 8, 2)]
    [InlineData("1d20+4", 1, 20, 4)]
    [InlineData("d6", 1, 6, 0)]
    [InlineData("3d6-1", 3, 6, -1)]
    public void Parses_expressions(string input, int count, int sides, int mod)
    {
        var e = DiceExpression.Parse(input);
        Assert.Equal((count, sides, mod), (e.Count, e.Sides, e.Modifier));
    }

    [Fact]
    public void Roll_is_within_bounds_and_deterministic_with_seed()
    {
        var roller = new DiceRoller(new Random(42));
        var r = roller.Roll(DiceExpression.Parse("2d8+2"), "Cure Wounds");
        Assert.Equal(2, r.Dice.Count);
        Assert.InRange(r.Total, 2 * 1 + 2, 2 * 8 + 2);
        Assert.All(r.Dice, d => Assert.InRange(d.Value, 1, 8));
    }
}
