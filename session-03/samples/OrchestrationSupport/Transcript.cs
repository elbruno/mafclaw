// Objective: display concurrent orchestration evidence without interleaved console lines.
// Steps:
// A. Bound individual event text and retained history.
// B. Record and print each event under one lock.
// C. Return an immutable snapshot for local verification.

namespace MafClaw.OrchestrationSupport;

public sealed class Transcript(TextWriter output)
{
    private readonly object sync = new();
    private readonly Queue<TranscriptEntry> entries = new();

    public IReadOnlyList<TranscriptEntry> Entries
    {
        get { lock (sync) { return entries.ToArray(); } }
    }

    public void Write(string agent, string state, string detail)
    {
        // A. This is a teaching transcript, not an unbounded production log.
        const int limit = 6000;
        var displayed = detail.Length <= limit ? detail : detail[..limit] + " [transcript excerpt truncated]";
        var entry = new TranscriptEntry(DateTimeOffset.UtcNow, agent, state, displayed);

        // B/C. Preserve the relationship between the recorded event and its visible line.
        lock (sync)
        {
            if (entries.Count == 1000)
            {
                entries.Dequeue();
            }
            entries.Enqueue(entry);
            output.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] {agent} | {state} | {displayed}");
        }
    }
}
