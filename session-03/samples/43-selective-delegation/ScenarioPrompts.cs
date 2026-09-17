// Objective: contrast zero, one and two delegated specialists.
// A. Define the finite classroom walkthrough prompts.
// B. Keep the exact offline dispatch map explicitly labelled scripted.
// C. Let live inference choose freely from worker descriptions instead.

namespace MafClaw.Sample43;

public static class ScenarioPrompts
{
    public const string Explanation = "Explain diversification without inspecting my portfolio or looking up news.";
    public const string News = "Summarize the educational news only; do not analyze my portfolio.";
    public const string Portfolio = "Analyze my mock portfolio allocation and risk; no news is needed.";
    public const string MorningBrief = "Prepare my morning brief: news, mock portfolio allocation, and risk.";
    public static IReadOnlyList<string> Demo { get; } = [Explanation, News, Portfolio];

    // Validate documented scenario outcomes without replacing live model-based dispatch.
    public static string[]? RequiredWorkers(string prompt) => prompt.Trim() switch
    {
        Explanation => [],
        News => ["NewsAgent"],
        Portfolio => ["AllocationAgent", "RiskAgent"],
        MorningBrief => ["NewsAgent", "AllocationAgent", "RiskAgent"],
        _ => null
    };

    public static string[] ScriptedWorkers(string prompt) => RequiredWorkers(prompt)
        ?? throw new ArgumentException("Fixture mode accepts only the documented exact demo prompts; it is scripted, not live inference.");
}
