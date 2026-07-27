using System.Text.RegularExpressions;

namespace DndCompanion.Application.Dice;

/// <summary>
/// A parsed dice spec like "2d8+2", "1d20+4", "d6", "3d6-1".
/// Pure/deterministic parsing — actual rolling lives behind IDiceRoller
/// so it can be seeded/mocked in tests.
/// </summary>
public sealed partial record DiceExpression(int Count, int Sides, int Modifier)
{
    [GeneratedRegex(@"^\s*(?<count>\d*)d(?<sides>\d+)\s*(?<mod>[+-]\s*\d+)?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex Pattern();

    public static bool TryParse(string input, out DiceExpression? expr)
    {
        expr = null;
        var m = Pattern().Match(input ?? "");
        if (!m.Success) return false;

        var count = m.Groups["count"].Value is "" ? 1 : int.Parse(m.Groups["count"].Value);
        var sides = int.Parse(m.Groups["sides"].Value);
        var mod = 0;
        if (m.Groups["mod"].Success)
            mod = int.Parse(m.Groups["mod"].Value.Replace(" ", ""));

        if (count is < 1 or > 100 || sides is < 2 or > 1000) return false;

        expr = new DiceExpression(count, sides, mod);
        return true;
    }

    public static DiceExpression Parse(string input) =>
        TryParse(input, out var e) ? e! : throw new FormatException($"Bad dice expression: '{input}'");

    public override string ToString()
    {
        var sign = Modifier == 0 ? "" : Modifier > 0 ? $"+{Modifier}" : Modifier.ToString();
        return $"{Count}d{Sides}{sign}";
    }
}
