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
