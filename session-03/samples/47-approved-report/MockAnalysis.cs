// Objective: supply read-only educational observations to the analysis workers.
// A. Read this assembly's embedded fixture.
// B. Select the exact source assigned to each worker.
// C. Label every observation fictional and non-advisory.

using System.Text.Json;

namespace MafClaw.Sample47;

public static class MockAnalysis
{
    public static string For(string reference)
    {
        using var stream = typeof(MockAnalysis).Assembly.GetManifestResourceStream("Sample47.MockAnalysis.json")
            ?? throw new InvalidOperationException("The educational fixture is missing.");
        var observations = JsonSerializer.Deserialize<AnalysisObservation[]>(stream)
            ?? throw new InvalidOperationException("The educational fixture is invalid.");
        var observation = observations.Single(item => item.Reference == reference);
        return $"FICTIONAL EDUCATIONAL DATA. Not financial advice.\n[{observation.Reference}] {observation.Observation}";
    }
}
