// Objective: run the complete Session 4 finance agent through an evaluation host, not a chat UI.
// A. Require an explicit mode.
// B. Run the same factory and published MAF evaluator API.
// C. Persist a safe report and fail the process for failed evaluations.

using System.Security.Cryptography;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using MafClaw.Session04;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

try
{
    // A. Fixture scripts inference; live uses a model with local grading; foundry also grades remotely.
    var mode = args.Length >= 2 && args[0] == "--mode" ? args[1] : "";
    if (mode is not ("fixture" or "live" or "foundry") ||
        args.Skip(2).Any(argument => argument != "--inject-regression"))
        throw new FinanceConfigurationException("Usage: --mode fixture|live|foundry [--inject-regression]");
    var inject = args.Contains("--inject-regression");
    if (inject && mode != "fixture")
        throw new FinanceConfigurationException("Regression injection is restricted to fixture mode.");
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));

    // B. First verify deterministic contracts, then grade a conversation from the same finance factory.
    var checks = await FinanceContractChecks.RunAsync(cancellationToken: deadline.Token);
    var settings = mode == "fixture" ? null : FinanceSettings.Load();
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = mode == "fixture" ? FinanceHostProfile.Fixture : FinanceHostProfile.Local,
        Settings = settings, ChatClient = mode == "fixture" ? ScriptedChatClient.Portfolio(inject) : null,
        EnableShell = false, EnableCodeAct = false, EnableMemory = false, EnableResearch = false
    }, deadline.Token);

    // MAF's IAgentEvaluator/EvaluateAsync separate running an agent from judging its output.
    // FoundryEvals handles remote grading integration; LocalEvaluator keeps grading in-process.
    IAgentEvaluator evaluator = mode == "foundry"
        ? new FoundryEvals(new AIProjectClient(settings!.ProjectEndpoint, new AzureCliCredential()),
            settings.Model, FoundryEvals.Relevance, FoundryEvals.Coherence)
        : FinanceEvaluations.CreateLocalEvaluator();
    var evaluation = await build.Agent.EvaluateAsync([FinanceEvaluations.Query], evaluator,
        cancellationToken: deadline.Token);

    // C. Both kinds of checks must pass. Store reproducible version/outcome evidence, not conversation text.
    var passed = checks.All(check => check.Passed) && evaluation.AllPassed;
    var dataset = Path.Combine(AppContext.BaseDirectory, "evaluation-cases.json");
    var report = new
    {
        schemaVersion = 1, mode, injectedRegression = inject, createdUtc = DateTimeOffset.UtcNow,
        datasetSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(dataset, deadline.Token))),
        assemblyVersion = typeof(FinanceAgentFactory).Assembly.GetName().Version?.ToString(),
        frameworkVersion = typeof(AIAgent).Assembly.GetName().Version?.ToString(),
        model = settings?.Model ?? "scripted",
        contracts = checks, agentEvaluation = new { evaluation.Passed, evaluation.Failed, evaluation.Total },
        status = passed ? "Pass" : "Fail"
    };
    var output = Path.Combine(Environment.CurrentDirectory, ".local", "evaluations");
    Directory.CreateDirectory(output);
    await File.WriteAllTextAsync(Path.Combine(output, $"session04-{mode}-{Guid.NewGuid():N}.json"),
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), deadline.Token);
    Console.WriteLine($"CONTRACTS: {checks.Count(check => check.Passed)}/{checks.Count}");
    Console.WriteLine($"MAF EVALUATION: {evaluation.Passed}/{evaluation.Total}; MODE: {mode}");

    // The failing process exit lets automation reject a candidate even when inference completed.
    Console.WriteLine(passed ? "EVALUATION PASS" : "EVALUATION FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception)
{
    return SafeErrors.Report(exception);
}
