namespace DndCompanion.Application.Encounters;

public static class TurnOrderCalculator
{
    /// <summary>
    /// Advances to the next non-defeated combatant in turn order, wrapping to the next round
    /// when the order overflows. Bounded to at most one full lap of the order, so an all-
    /// defeated encounter returns the input unchanged instead of looping forever.
    /// </summary>
    public static (int NextTurnIndex, int NextRound) Advance(
        IReadOnlyList<bool> defeatedInOrder, int currentTurnIndex, int currentRound)
    {
        var count = defeatedInOrder.Count;
        if (count == 0) return (currentTurnIndex, currentRound);

        var index = currentTurnIndex;
        var round = currentRound;
        for (var i = 0; i < count; i++)
        {
            index++;
            if (index >= count)
            {
                index = 0;
                round++;
            }
            if (!defeatedInOrder[index]) return (index, round);
        }

        // Every combatant is defeated — nowhere to advance to.
        return (currentTurnIndex, currentRound);
    }
}
