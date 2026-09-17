// Objective: demonstrate report approval with scripted, clearly labelled inference.
// A. Return known worker findings through real AsAIAgent calls.
// B. Make the scripted main consume the actual tool-result hash.
// C. Keep human/simulated console input outside model messages.

using System.Text.RegularExpressions;
using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample47;

public static class FixtureClients
{
    public const string ReportBody =
        "SCRIPTED report: the classroom exercise assigns 60 growth tokens and 40 reserve tokens [MOCK-ALLOCATION]. " +
        "Concentration is a discussion topic; returns and probabilities are not supplied [MOCK-RISK].";
    public static string ReportHash => ReportStore.Hash(ReportStore.Label + ReportBody);

    public static FixtureChatClient Worker(string name) => new((_, _, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Text(name == "AllocationWorker"
            ? "SCRIPTED worker finding: 60 + 40 = 100 classroom tokens [MOCK-ALLOCATION]. Not financial advice."
            : "SCRIPTED worker finding: concentration warrants classroom discussion, but no risk probabilities are supplied [MOCK-RISK]. Not financial advice."));
    });

    public static FixtureChatClient CreateMain()
    {
        var step = 0;
        return new FixtureChatClient((messages, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = step++;
            return Task.FromResult(current switch
            {
                0 => Call(current, "analyze_allocation"),
                1 => Call(current, "analyze_risk"),
                2 => Call(current, "propose_report", new Dictionary<string, object?> { ["report"] = ReportBody }),
                3 => Call(current, "save_report", new Dictionary<string, object?> { ["reportHash"] = FindActualHash(messages) }),
                _ => Text("SCRIPTED narrative: inspect actual worker-result/file-result events and independent host verification.")
            });
        });
    }

    private static string FindActualHash(IReadOnlyList<ChatMessage> messages)
    {
        var result = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
            .Select(content => content.Result?.ToString() ?? "").Last(text => text.Contains("PROPOSED, NOT SAVED.", StringComparison.Ordinal));
        var match = Regex.Match(result, @"SHA256=([A-F0-9]{64})");
        return match.Success ? match.Groups[1].Value : throw new InvalidOperationException("Fixture did not receive the host proposal hash.");
    }

    private static ChatResponse Call(int index, string name, Dictionary<string, object?>? arguments = null) =>
        new(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent($"fixture47-{index}", name, arguments ?? new Dictionary<string, object?>())]));

    private static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));
}
