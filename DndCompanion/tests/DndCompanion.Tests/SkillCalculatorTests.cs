using DndCompanion.Application.Rules;
using DndCompanion.Domain;
using DndCompanion.Domain.ValueObjects;

namespace DndCompanion.Tests;

public class SkillCalculatorTests
{
    private static readonly AbilityScores Abilities = new(
        Strength: 14, Dexterity: 16, Constitution: 12, Intelligence: 10, Wisdom: 18, Charisma: 8);

    [Fact]
    public void Not_proficient_is_just_the_ability_modifier()
    {
        // Athletics -> STR (mod +2), no proficiency
        Assert.Equal(2, SkillCalculator.Modifier(Abilities, Skill.Athletics, proficiencyBonus: 3, proficient: false, expert: false));
    }

    [Fact]
    public void Proficient_adds_proficiency_bonus_once()
    {
        Assert.Equal(5, SkillCalculator.Modifier(Abilities, Skill.Athletics, proficiencyBonus: 3, proficient: true, expert: false));
    }

    [Fact]
    public void Expert_adds_proficiency_bonus_twice()
    {
        Assert.Equal(8, SkillCalculator.Modifier(Abilities, Skill.Athletics, proficiencyBonus: 3, proficient: true, expert: true));
    }

    [Fact]
    public void Expert_without_proficient_flag_still_doubles()
    {
        // Expert implies proficient in practice, but the calculator itself just trusts the flag.
        Assert.Equal(8, SkillCalculator.Modifier(Abilities, Skill.Athletics, proficiencyBonus: 3, proficient: false, expert: true));
    }

    [Theory]
    [InlineData(Skill.Athletics, 2)]        // STR mod
    [InlineData(Skill.Stealth, 3)]          // DEX mod
    [InlineData(Skill.Arcana, 0)]           // INT mod
    [InlineData(Skill.Insight, 4)]          // WIS mod
    [InlineData(Skill.Persuasion, -1)]      // CHA mod
    public void Uses_the_correct_governing_ability_per_skill(Skill skill, int expectedMod)
    {
        Assert.Equal(expectedMod, SkillCalculator.Modifier(Abilities, skill, proficiencyBonus: 3, proficient: false, expert: false));
    }

    [Fact]
    public void Saving_throw_modifier_adds_proficiency_when_proficient()
    {
        Assert.Equal(7, SkillCalculator.SavingThrowModifier(Abilities, Ability.Wisdom, proficiencyBonus: 3, proficient: true));
        Assert.Equal(4, SkillCalculator.SavingThrowModifier(Abilities, Ability.Wisdom, proficiencyBonus: 3, proficient: false));
    }
}
