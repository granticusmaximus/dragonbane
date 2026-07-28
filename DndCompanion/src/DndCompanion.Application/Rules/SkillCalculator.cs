using DndCompanion.Domain;
using DndCompanion.Domain.ValueObjects;

namespace DndCompanion.Application.Rules;

public static class SkillCalculator
{
    public static int Modifier(AbilityScores abilities, Skill skill, int proficiencyBonus, bool proficient, bool expert)
    {
        var abilityMod = abilities.ModifierFor(SkillCatalog.GoverningAbility[skill]);
        var bonus = expert ? proficiencyBonus * 2 : proficient ? proficiencyBonus : 0;
        return abilityMod + bonus;
    }

    public static int SavingThrowModifier(AbilityScores abilities, Ability ability, int proficiencyBonus, bool proficient)
        => abilities.ModifierFor(ability) + (proficient ? proficiencyBonus : 0);
}
