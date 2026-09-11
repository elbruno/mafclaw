// Objective: compose the four Session 03 boundaries into one offline morning brief.
// Steps:
// A. Show the planned workflow so the presenter can narrate it.
// B. Run the watchlist, shell, and background operations in order.
// C. Return structured results and explicit safety guidance.

internal sealed class Session03Sample(
    Session03Settings settings,
    SkillCatalog skillCatalog,
    MockWatchlist watchlist,
    Session02Continuity session02Continuity,
    IShellTool shellTool,
    IBackgroundTaskRunner backgroundTaskRunner)
{
    private readonly Session03Settings _settings = settings;
    private readonly SkillCatalog _skillCatalog = skillCatalog;
    private readonly MockWatchlist _watchlist = watchlist;
    private readonly Session02Continuity _session02Continuity = session02Continuity;
    private readonly IShellTool _shellTool = shellTool;
    private readonly IBackgroundTaskRunner _backgroundTaskRunner = backgroundTaskRunner;

    public async Task<Session03Result> RunAsync()
    {
        Console.WriteLine("Planned steps");
        foreach (var step in new[]
        {
            "Inspect the mock watchlist",
            "Run an allowlisted workspace check",
            "Queue a background volatility note",
            "Return completed and queued status"
        })
        {
            Console.WriteLine($"- {step}");
        }

        Console.WriteLine();

        var portfolio = _watchlist.Summarize();
        var shellResult = await _shellTool.RunAsync(_settings.Shell.Command);
        var backgroundTask = await _backgroundTaskRunner.QueueAsync(
            "background-research",
            "Prepare a mock market-volatility note for the next turn.");

        return new Session03Result(
            "03",
            new Session02Boundaries(
                _session02Continuity.WorkingFolder,
                _session02Continuity.FileAccess,
                _session02Continuity.Approval,
                _session02Continuity.Memory),
            ["skills", "shell", "codeact", "background_agents"],
            _skillCatalog.Skills,
            portfolio,
            shellResult,
            backgroundTask,
            [
                "Shell access is restricted to the configured command and workspace.",
                "Background work has an observable ticket and status; queued is not completed.",
                "All portfolio values are mock educational data."
            ]);
    }
}

internal sealed record Session03Result(
    string Session,
    Session02Boundaries Session02Boundaries,
    IReadOnlyList<string> FeatureSet,
    IReadOnlyList<SkillDefinition> Skills,
    PortfolioSummary Portfolio,
    ShellResult Shell,
    BackgroundTaskTicket BackgroundTask,
    IReadOnlyList<string> Guidance);

internal sealed record Session02Boundaries(
    string WorkingFolder,
    string FileAccess,
    string Approval,
    string Memory);
