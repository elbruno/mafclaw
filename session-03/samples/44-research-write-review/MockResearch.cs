// Objective: load only this assembly's educational research fixture.
// A. Read embedded mock data, never the web or a model-selected path.
// B. Deserialize the source identifiers and observations.
// C. Format the exact evidence passed to the WriterAgent.

using System.Text.Json;

namespace MafClaw.Sample44;

public static class MockResearch
{
    public static IReadOnlyList<ResearchSource> Load()
    {
        using var stream = typeof(MockResearch).Assembly.GetManifestResourceStream("Sample44.MockSources.json")
            ?? throw new InvalidOperationException("The educational fixture is missing.");
        return JsonSerializer.Deserialize<ResearchSource[]>(stream)
            ?? throw new InvalidOperationException("The educational fixture is invalid.");
    }

    public static string Format(IReadOnlyList<ResearchSource> sources) =>
        "FICTIONAL EDUCATIONAL SOURCES — not news, prices, or trading signals.\n" +
        string.Join("\n", sources.Select(source => $"[{source.Id}] {source.Observation}"));
}
