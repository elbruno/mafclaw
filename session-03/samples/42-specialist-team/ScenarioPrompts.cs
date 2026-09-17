// Objective: publish the exact bounded classroom walkthrough prompts.
// A. Demonstrate the mandatory three-specialist morning brief.
// B. Offer smaller questions for live model-based selection.
// C. Label the offline prompt-to-role map as a script, not inference.

namespace MafClaw.Sample42;

public static class ScenarioPrompts
{
    public const string MorningBrief = "Prepare my morning brief: news, mock portfolio allocation, and risk.";
    public const string Explanation = "Explain diversification without inspecting my portfolio or looking up news.";
    public const string News = "Summarize the educational news only; do not analyze my portfolio.";
    public const string Portfolio = "Analyze my mock portfolio allocation and risk; no news is needed.";
    public static IReadOnlyList<string> Demo { get; } = [MorningBrief];

    // This is a completion contract, not a live dispatcher; the model still chooses tools.
    public static string[]? RequiredWorkers(string prompt) => prompt.Trim() switch
    {
        MorningBrief => ["NewsAgent", "AllocationAgent", "RiskAgent"],
        Explanation => [],
        News => ["NewsAgent"],
        Portfolio => ["AllocationAgent", "RiskAgent"],
        _ => null
    };

    public static string[] ScriptedWorkers(string prompt) => RequiredWorkers(prompt)
        ?? throw new ArgumentException("Fixture mode accepts only the documented exact demo prompts; it is scripted, not live inference.");
}
