using DndCompanion.Domain;

namespace DndCompanion.Application.Rules;

/// <summary>
/// Skill-to-governing-ability mapping — fixed 5e mechanical structure, not proprietary
/// descriptive rules text, so it's safe to hardcode (unlike spell/monster descriptions).
/// </summary>
public static class SkillCatalog
{
    public static readonly IReadOnlyDictionary<Skill, Ability> GoverningAbility = new Dictionary<Skill, Ability>
    {
        [Skill.Acrobatics] = Ability.Dexterity,
        [Skill.AnimalHandling] = Ability.Wisdom,
        [Skill.Arcana] = Ability.Intelligence,
        [Skill.Athletics] = Ability.Strength,
        [Skill.Deception] = Ability.Charisma,
        [Skill.History] = Ability.Intelligence,
        [Skill.Insight] = Ability.Wisdom,
        [Skill.Intimidation] = Ability.Charisma,
        [Skill.Investigation] = Ability.Intelligence,
        [Skill.Medicine] = Ability.Wisdom,
        [Skill.Nature] = Ability.Intelligence,
        [Skill.Perception] = Ability.Wisdom,
        [Skill.Performance] = Ability.Charisma,
        [Skill.Persuasion] = Ability.Charisma,
        [Skill.Religion] = Ability.Intelligence,
        [Skill.SleightOfHand] = Ability.Dexterity,
        [Skill.Stealth] = Ability.Dexterity,
        [Skill.Survival] = Ability.Wisdom,
    };

    /// <summary>All 18 skills in a stable display order.</summary>
    public static readonly IReadOnlyList<Skill> All =
    [
        Skill.Acrobatics, Skill.AnimalHandling, Skill.Arcana, Skill.Athletics,
        Skill.Deception, Skill.History, Skill.Insight, Skill.Intimidation,
        Skill.Investigation, Skill.Medicine, Skill.Nature, Skill.Perception,
        Skill.Performance, Skill.Persuasion, Skill.Religion,
        Skill.SleightOfHand, Skill.Stealth, Skill.Survival
    ];
}
