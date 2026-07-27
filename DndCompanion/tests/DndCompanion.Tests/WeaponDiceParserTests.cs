using DndCompanion.Application.Dice;

namespace DndCompanion.Tests;

public class WeaponDiceParserTests
{
    [Theory]
    [InlineData("""{"weaponType":"Simple Melee","damage":"1d4 Bludgeoning","properties":"Light","mastery":"Slow"}""", "1d4")]
    [InlineData("""{"weaponType":"Martial Melee","damage":"1d8 Slashing","properties":"Versatile (1d10)","mastery":"Sap"}""", "1d8")]
    [InlineData("""{"weaponType":"Simple Ranged","damage":"1d6 Piercing"}""", "1d6")]
    public void Extracts_leading_dice_token_from_damage_field(string json, string expected)
    {
        Assert.Equal(expected, WeaponDiceParser.ExtractDamageDice(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"armorType":"Light Armor","armorClass":"11 + Dex modifier"}""")] // armor row, no "damage" key
    [InlineData("""{"damage":null}""")]
    [InlineData("""{"damage":"see rules text"}""")] // not a parseable dice expression
    public void Returns_null_for_missing_or_unparseable_damage(string? json)
    {
        Assert.Null(WeaponDiceParser.ExtractDamageDice(json));
    }
}
