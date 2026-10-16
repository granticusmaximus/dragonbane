namespace DndCompanion.Application.Encounters;

public static class EncounterTurnLogFormatter
{
    public static string FormatAction(string action, string? description)
    {
        var cleanAction = action.Trim();
        var cleanDescription = description?.Trim();

        if (string.IsNullOrWhiteSpace(cleanAction))
            return cleanDescription ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleanDescription))
            return cleanAction;

        return $"{cleanAction}: {cleanDescription}";
    }
}
