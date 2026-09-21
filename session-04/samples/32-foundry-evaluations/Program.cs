// Objective: distinguish remote quality grading from deterministic evaluation.
// A. Describe the service/cost prerequisite without submitting work.
// B. Explicitly invoke the published FoundryEvals integration.
// C. Report actual outcomes without inventing a hosted score.
using Azure.AI.Projects;
using Azure.Identity;
using MafClaw.Session04;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

if (args.Length == 0 || args.SequenceEqual(["--describe"]))
{
    Console.WriteLine("FOUNDRY EVALUATION PREREQUISITES: approved project, supported evaluation deployment, permissions and budget.");
    Console.WriteLine("No evaluation was submitted. Use --live only with authorization.");
    return 2;
}
try
{
    if (!args.SequenceEqual(["--live"])) throw new FinanceConfigurationException("Usage: --describe | --live");
    var settings = FinanceSettings.Load();
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Settings = settings, EnableShell = false, EnableCodeAct = false, EnableMemory = false, EnableResearch = false
    }, deadline.Token);
    var evaluator = new FoundryEvals(new AIProjectClient(settings.ProjectEndpoint, new AzureCliCredential()),
        settings.Model, FoundryEvals.Relevance, FoundryEvals.Coherence);
    var result = await build.Agent.EvaluateAsync([FinanceEvaluations.Query], evaluator, cancellationToken: deadline.Token);
    Console.WriteLine($"Foundry evaluator status: {result.Status}; cases passed: {result.Passed}/{result.Total}.");
    Console.WriteLine(result.AllPassed ? "FOUNDRY EVALUATION PASS" : "FOUNDRY EVALUATION FAIL");
    return result.AllPassed ? 0 : 1;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
