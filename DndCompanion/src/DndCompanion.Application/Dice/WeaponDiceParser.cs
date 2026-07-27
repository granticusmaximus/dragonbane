using System.Text.Json;

namespace DndCompanion.Application.Dice;

/// <summary>
/// Extracts a rollable dice expression from an Item's PropertiesJson "damage" field
/// (e.g. "1d8 Slashing" -> "1d8"). This JSON shape is a convention set by SrdImporter/DevSeed
/// when they seed weapon rows — items don't have a dedicated structured dice column yet.
/// </summary>
public static class WeaponDiceParser
{
    public static string? ExtractDamageDice(string? propertiesJson)
    {
        if (string.IsNullOrWhiteSpace(propertiesJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(propertiesJson);
            if (!doc.RootElement.TryGetProperty("damage", out var dmgEl)) return null;
            var dmg = dmgEl.GetString();
            if (string.IsNullOrWhiteSpace(dmg)) return null;

            var firstToken = dmg.Split(' ', 2)[0];
            return DiceExpression.TryParse(firstToken, out _) ? firstToken : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
