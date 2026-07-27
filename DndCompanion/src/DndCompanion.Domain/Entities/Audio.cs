namespace DndCompanion.Domain.Entities;

public class Recording
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionLogId { get; set; }
    public SessionLog SessionLog { get; set; } = null!;
    public DateTime StartedUtc { get; set; }
    public DateTime? EndedUtc { get; set; }
    public string AudioPath { get; set; } = "";

    public List<TranscriptSegment> Segments { get; set; } = [];
    public List<StructuredNote> Notes { get; set; } = [];
}

public class TranscriptSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordingId { get; set; }
    public Recording Recording { get; set; } = null!;
    public double TStart { get; set; }
    public double TEnd { get; set; }
    public string? SpeakerLabel { get; set; }      // often null — diarization is hard
    public string Text { get; set; } = "";
    public double? Confidence { get; set; }
}

/// <summary>AI-drafted note. Never auto-committed — Confirmed flips on user approval.</summary>
public class StructuredNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecordingId { get; set; }
    public Recording Recording { get; set; } = null!;
    public double TStart { get; set; }
    public NoteKind Kind { get; set; }
    public string? Subject { get; set; }
    public string Text { get; set; } = "";
    public bool Confirmed { get; set; }
}
