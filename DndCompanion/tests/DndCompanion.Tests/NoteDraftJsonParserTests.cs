using DndCompanion.Application.Notes;
using DndCompanion.Domain;

namespace DndCompanion.Tests;

public class NoteDraftJsonParserTests
{
    [Fact]
    public void Parses_well_formed_notes_array()
    {
        var json = """
            {"notes": [
                {"kind": "Loot", "subject": "Baloney Slim", "text": "Found a healing potion.", "tStart": 12.5},
                {"kind": "Location", "subject": null, "text": "The party entered the basement.", "tStart": 40.0}
            ]}
            """;

        var drafts = NoteDraftJsonParser.Parse(json);

        Assert.Equal(2, drafts.Count);
        Assert.Equal(NoteKind.Loot, drafts[0].Kind);
        Assert.Equal("Baloney Slim", drafts[0].Subject);
        Assert.Equal("Found a healing potion.", drafts[0].Text);
        Assert.Equal(12.5, drafts[0].TStart);
        Assert.Equal(NoteKind.Location, drafts[1].Kind);
        Assert.Null(drafts[1].Subject);
    }

    [Fact]
    public void Empty_notes_array_returns_empty_list()
    {
        Assert.Empty(NoteDraftJsonParser.Parse("""{"notes": []}"""));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"somethingElse": 1}""")]
    public void Malformed_or_unexpected_shape_degrades_to_empty_list(string json)
    {
        Assert.Empty(NoteDraftJsonParser.Parse(json));
    }

    [Fact]
    public void Unknown_kind_defaults_to_other()
    {
        var json = """{"notes": [{"kind": "Something Weird", "text": "x", "tStart": 1.0}]}""";

        var drafts = NoteDraftJsonParser.Parse(json);

        Assert.Single(drafts);
        Assert.Equal(NoteKind.Other, drafts[0].Kind);
    }

    [Fact]
    public void Kind_parsing_is_case_insensitive()
    {
        var json = """{"notes": [{"kind": "loot", "text": "x", "tStart": 1.0}]}""";

        Assert.Equal(NoteKind.Loot, NoteDraftJsonParser.Parse(json)[0].Kind);
    }

    [Fact]
    public void Entries_with_blank_text_are_skipped()
    {
        var json = """{"notes": [{"kind": "Other", "text": "   ", "tStart": 1.0}, {"kind": "Other", "text": "kept", "tStart": 2.0}]}""";

        var drafts = NoteDraftJsonParser.Parse(json);

        Assert.Single(drafts);
        Assert.Equal("kept", drafts[0].Text);
    }
}
