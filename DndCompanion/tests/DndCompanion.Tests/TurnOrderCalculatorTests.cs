using DndCompanion.Application.Encounters;

namespace DndCompanion.Tests;

public class TurnOrderCalculatorTests
{
    [Fact]
    public void Advances_to_the_next_combatant_in_order()
    {
        var (index, round) = TurnOrderCalculator.Advance([false, false, false], currentTurnIndex: 0, currentRound: 1);
        Assert.Equal(1, index);
        Assert.Equal(1, round);
    }

    [Fact]
    public void Wraps_to_the_next_round_after_the_last_combatant()
    {
        var (index, round) = TurnOrderCalculator.Advance([false, false, false], currentTurnIndex: 2, currentRound: 1);
        Assert.Equal(0, index);
        Assert.Equal(2, round);
    }

    [Fact]
    public void Skips_defeated_combatants()
    {
        var (index, round) = TurnOrderCalculator.Advance([false, true, false], currentTurnIndex: 0, currentRound: 1);
        Assert.Equal(2, index);
        Assert.Equal(1, round);
    }

    [Fact]
    public void Skips_defeated_combatants_across_a_round_wrap()
    {
        // Only index 1 survives; starting there, the search must skip index 2 (defeated),
        // wrap to a new round, skip index 0 (defeated), and land back on index 1.
        var (index, round) = TurnOrderCalculator.Advance([true, false, true], currentTurnIndex: 1, currentRound: 3);
        Assert.Equal(1, index);
        Assert.Equal(4, round);
    }

    [Fact]
    public void All_defeated_returns_input_unchanged_and_does_not_loop_forever()
    {
        var (index, round) = TurnOrderCalculator.Advance([true, true, true], currentTurnIndex: 0, currentRound: 5);
        Assert.Equal(0, index);
        Assert.Equal(5, round);
    }

    [Fact]
    public void Empty_order_returns_input_unchanged()
    {
        var (index, round) = TurnOrderCalculator.Advance([], currentTurnIndex: 0, currentRound: 1);
        Assert.Equal(0, index);
        Assert.Equal(1, round);
    }
}
