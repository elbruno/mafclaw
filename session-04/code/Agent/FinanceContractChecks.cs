// Objective: run the synthetic dataset independently of model prose.
// A. Load a versioned case corpus.
// B. Check arithmetic, policy and actual MAF capability registration.
// C. Return explicit per-case results; unknown checks fail.
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public static class FinanceContractChecks
{
    public static async Task<IReadOnlyList<FinanceCheckResult>> RunAsync(
        string? dataset = null, CancellationToken cancellationToken = default)
    {
        dataset ??= Path.Combine(AppContext.BaseDirectory, "evaluation-cases.json");
        var cases = JsonSerializer.Deserialize<FinanceEvaluationCase[]>(await File.ReadAllTextAsync(dataset, cancellationToken),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("Dataset is empty.");
        if (cases.Length < 24 || cases.Select(item => item.Id).Distinct().Count() != cases.Length)
            throw new InvalidDataException("The dataset requires at least 24 uniquely identified cases.");
        var results = new List<FinanceCheckResult>();
        foreach (var item in cases)
        {
            var observed = await ObserveAsync(item, cancellationToken);
            results.Add(new(item.Id, observed == item.Expected,
                observed == item.Expected ? "Expected behavior observed." : "Observed behavior differs from the contract."));
        }
        return results;
    }

    private static async Task<string> ObserveAsync(FinanceEvaluationCase item, CancellationToken cancellationToken)
    {
        string Number(decimal value) => value.ToString("F2", CultureInfo.InvariantCulture);
        switch (item.Kind)
        {
            case "total": return Number(MockPortfolio.Summarize().Total);
            case "technology": return Number(MockPortfolio.Summarize().TechnologyPercent);
            case "holding": return Number(MockPortfolio.Holdings.Single(row => row.Symbol == item.Input).Value);
            case "empty-total": return Number(MockPortfolio.Summarize([]).Total);
            case "empty-technology": return Number(MockPortfolio.Summarize([]).TechnologyPercent);
            case "quote": return new FinanceTools().GetStockPrice(item.Input);
            case "policy": return FinancePolicy.GetBlockingRule(item.Input) ?? "allow";
            case "invalid-trade":
                var arguments = item.Input.Split('|');
                var tools = new FinanceTools();
                try { tools.SimulateTrade(arguments[0], arguments[1], int.Parse(arguments[2], CultureInfo.InvariantCulture)); }
                catch (ArgumentException) { return tools.ExecutedTrades.Count == 0 ? "denied" : "side-effect"; }
                return "unexpectedly-allowed";
            case "hosted-profile":
            case "fixture-memory":
                var client = new ScriptedChatClient(ScriptedChatClient.Text("Ready."));
                await using (var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
                {
                    Profile = item.Kind == "hosted-profile" ? FinanceHostProfile.Hosted : FinanceHostProfile.Fixture,
                    ChatClient = client, EnableResearch = false
                }, cancellationToken))
                {
                    var session = await build.CreateSessionAsync(cancellationToken);
                    await build.Agent.RunAsync("Say ready.", session, cancellationToken: cancellationToken);
                    var names = client.LastFunctions.Select(function => function.Name).ToArray();
                    var forbidden = names.Any(name => name.StartsWith("file_access", StringComparison.Ordinal) ||
                        name.StartsWith("file_memory", StringComparison.Ordinal) || name == "run_shell" ||
                        name.Contains("execute_code", StringComparison.Ordinal) ||
                        (item.Kind == "hosted-profile" && name == "request_simulated_trade"));
                    return forbidden ? "unexpected-capability" : item.Kind == "hosted-profile" ? "restricted" : "disabled";
                }
            case "stream-policy":
                using (var governed = new GovernedChatClient(new ScriptedChatClient(ScriptedChatClient.Text(item.Input))))
                {
                    var text = "";
                    await foreach (var update in governed.GetStreamingResponseAsync(
                        [new ChatMessage(ChatRole.User, "Return the synthetic fixture.")], cancellationToken: cancellationToken))
                        text += update.Text;
                    return text == FinancePolicy.BlockMessage && !text.Contains(FinancePolicy.RestrictedMarker) ? "blocked" : "leaked";
                }
            default: throw new InvalidDataException("Dataset contains an unsupported check kind.");
        }
    }
}
