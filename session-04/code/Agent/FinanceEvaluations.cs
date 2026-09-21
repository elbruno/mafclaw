// Objective: use the published MAF evaluator API on actual agent conversations.
// A. Check exact numeric answers and an actual valuation tool result.
// B. Keep local grading separate from the cost of live inference.
// C. Add Foundry model grading only through its explicit evaluator.
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public static class FinanceEvaluations
{
    public const string Query = "Use value_portfolio to give the fixed snapshot total and Technology percentage. State the mock-data disclaimer.";

    public static LocalEvaluator CreateLocalEvaluator() => new(
        FunctionEvaluator.Create("exact_snapshot", (EvalItem item) =>
            Regex.IsMatch(item.Response, @"(?<![\d.])(?:27,124\.95|27124\.95)(?!\d)") &&
            Regex.IsMatch(item.Response, @"(?<![\d.])66\.01(?!\d)")),
        FunctionEvaluator.Create("mock_disclaimer", (EvalItem item) =>
            item.Response.Contains("mock", StringComparison.OrdinalIgnoreCase)),
        FunctionEvaluator.Create("actual_valuation_tool", (EvalItem item) =>
            item.Conversation.SelectMany(message => message.Contents)
                .OfType<FunctionResultContent>().Any(result => HasExactSnapshot(result.Result))));

    private static bool HasExactSnapshot(object? result)
    {
        if (result is PortfolioSummary summary)
            return summary.Total == 27124.95m && summary.TechnologyPercent == 66.01m;
        var element = JsonSerializer.SerializeToElement(result);
        if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString();
            if (string.IsNullOrWhiteSpace(text) || !text.TrimStart().StartsWith('{')) return false;
            try
            {
                using var document = JsonDocument.Parse(text);
                element = document.RootElement.Clone();
            }
            catch (JsonException) { return false; }
        }
        if (element.ValueKind != JsonValueKind.Object) return false;
        var properties = element.EnumerateObject().ToDictionary(property => property.Name,
            property => property.Value, StringComparer.OrdinalIgnoreCase);
        return properties.TryGetValue("Total", out var total) && total.ValueKind == JsonValueKind.Number &&
            total.TryGetDecimal(out var totalValue) &&
            properties.TryGetValue("TechnologyPercent", out var percent) && percent.ValueKind == JsonValueKind.Number &&
            percent.TryGetDecimal(out var percentage) &&
            totalValue == 27124.95m && percentage == 66.01m;
    }
}
