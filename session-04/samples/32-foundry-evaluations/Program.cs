// Objective: Sample 32 (MAF) contrasts remote quality grading with Sample 31's local predicates.
// A. Describe the service and budget prerequisites without submitting an evaluation.
// B. Build a generic explainer agent and select FoundryEvals criteria visibly.
// C. Run the evaluation and report the service's actual result.

using Azure.AI.Projects;
using Azure.Identity;
using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;

// A. Exit 2 means no service was exercised, not that an evaluation passed.
if (args.SequenceEqual(["--describe"]))
{
    Console.WriteLine("FOUNDRY EVALUATION PREREQUISITES: approved project, supported evaluation deployment, permissions and budget.");
    Console.WriteLine("No evaluation was submitted. Run the demo only with authorization.");
    return 2;
}

try
{
    if (args.Length != 0 && !args.SequenceEqual(["--live"]))
        throw new InvalidOperationException("Run with dotnet run. Offline preview: --describe.");
    if (args.Length == 0)
    {
        Console.Write("This submits a paid Foundry evaluation. Prerequisites approved? [y/N] ");
        if (!string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("No evaluation was submitted.");
            return 2;
        }
    }
    var settings = DemoSettings.Load();
    using var model = settings.CreateChatClient();

    // B. Agent construction is here; FoundryEvals supplies the grading-service integration.
    var agent = model.AsAIAgent(name: "WorkshopExplainer",
        instructions: "Explain agent framework concepts clearly in at most two sentences.");
    var evaluator = new FoundryEvals(new AIProjectClient(settings.ProjectEndpoint, new AzureCliCredential()),
        settings.Model, FoundryEvals.Relevance, FoundryEvals.Coherence);

    // C. Relevance/coherence are different checks from exact arithmetic or tool execution.
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
    var result = await agent.EvaluateAsync(["How does an agent harness differ from a model client?"], evaluator,
        cancellationToken: deadline.Token);
    Console.WriteLine($"Foundry evaluator status: {result.Status}; cases passed: {result.Passed}/{result.Total}.");
    Console.WriteLine(result.AllPassed ? "FOUNDRY EVALUATION PASS" : "FOUNDRY EVALUATION FAIL");
    return result.AllPassed ? 0 : 1;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
