using DndCompanion.Application.Encounters;

namespace DndCompanion.Tests;

public class EncounterTurnLogFormatterTests
{
    [Fact]
    public void FormatAction_returns_action_and_description_when_present()
    {
        var result = EncounterTurnLogFormatter.FormatAction("Attack", "Hit for 12 damage.");

        Assert.Equal("Attack: Hit for 12 damage.", result);
    }

    [Fact]
    public void FormatAction_uses_action_only_when_description_missing()
    {
        var result = EncounterTurnLogFormatter.FormatAction("Move", "   ");

        Assert.Equal("Move", result);
    }
}
