// Objective: exercise actual MAF orchestration without Azure or configuration.
// A. Inject explicitly scripted main-agent tool calls.
// B. Return labelled scripted writer/reviewer outputs.
// C. Leave every transition and limit to the production host wrappers.

using MafClaw.OrchestrationSupport;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample44;

public static class FixtureClients
{
    public static FixtureChatClient CreateMain()
    {
        string[] steps = ["research_sources", "write_draft", "review_draft", "revise_draft", "review_draft", "finish_report"];
        var step = 0;
        return new FixtureChatClient((_, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = step++;
            return Task.FromResult(index < steps.Length
                ? new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent($"fixture44-{index}", steps[index], new Dictionary<string, object?>())]))
                : Text("SCRIPTED narrative: consult the actual host workflow and specialist-result events above."));
        });
    }

    public static FixtureChatClient Writer()
    {
        var draft = 0;
        return new FixtureChatClient((_, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = ++draft == 1
                ? "SCRIPTED draft 1: the fictional growth basket has 60 classroom tokens [MOCK-ALPHA]. " +
                  "The reserve basket has 40 [MOCK-BETA]."
                : "SCRIPTED revised draft: the fictional growth basket has 60 classroom tokens [MOCK-ALPHA]; " +
                  "the reserve basket has 40 [MOCK-BETA]. Future returns are unknown. " + DraftCheck.RequiredDisclaimer;
            return Task.FromResult(Text(content));
        });
    }

    public static FixtureChatClient Reviewer()
    {
        var review = 0;
        return new FixtureChatClient((_, _, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Text(++review == 1
                ? $"SCRIPTED advisory review: add the missing disclaimer: {DraftCheck.RequiredDisclaimer}"
                : "SCRIPTED advisory review: the revision includes citations and disclaimer. This is not factual certification."));
        });
    }

    private static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));
}
