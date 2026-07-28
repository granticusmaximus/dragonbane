using DndCompanion.Domain;

namespace DndCompanion.Domain.ValueObjects;

public static class AbilityScoresExtensions
{
    public static int ModifierFor(this AbilityScores a, Ability ability) => ability switch
    {
        Ability.Strength => a.StrMod,
        Ability.Dexterity => a.DexMod,
        Ability.Constitution => a.ConMod,
        Ability.Intelligence => a.IntMod,
        Ability.Wisdom => a.WisMod,
        Ability.Charisma => a.ChaMod,
        _ => throw new ArgumentOutOfRangeException(nameof(ability))
    };
}
