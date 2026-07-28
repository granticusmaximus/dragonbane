namespace DndCompanion.Domain;

public enum ContentKind { Srd, Phb, Homebrew }

public enum ActionType { Action, BonusAction, Reaction, Movement, Free }

public enum SpellSchool
{
    Abjuration, Conjuration, Divination, Enchantment,
    Evocation, Illusion, Necromancy, Transmutation
}

public enum NoteKind { Turn, Move, Action, Location, Loot, Other }

public enum EntrySource { Manual, Dice, Audio }

// Medium listed first so it's the CLR/enum default (ordinal 0) — matches Character.Size's
// intended default and avoids an EF Core default-value "sentinel" mismatch: if Medium weren't
// the CLR default, explicitly setting a character to the actual CLR-default value (e.g. Tiny,
// a real size for familiars etc.) would be silently treated as "unset" and overwritten with
// the configured DB default at save time.
public enum SizeCategory { Medium, Tiny, Small, Large, Huge, Gargantuan }

[Flags]
public enum Ability
{
    None = 0,
    Strength = 1, Dexterity = 2, Constitution = 4,
    Intelligence = 8, Wisdom = 16, Charisma = 32
}

[Flags]
public enum Skill
{
    None = 0,
    Acrobatics = 1 << 0, AnimalHandling = 1 << 1, Arcana = 1 << 2, Athletics = 1 << 3,
    Deception = 1 << 4, History = 1 << 5, Insight = 1 << 6, Intimidation = 1 << 7,
    Investigation = 1 << 8, Medicine = 1 << 9, Nature = 1 << 10, Perception = 1 << 11,
    Performance = 1 << 12, Persuasion = 1 << 13, Religion = 1 << 14,
    SleightOfHand = 1 << 15, Stealth = 1 << 16, Survival = 1 << 17
}
