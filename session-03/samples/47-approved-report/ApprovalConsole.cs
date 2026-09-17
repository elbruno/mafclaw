// Objective: connect real MAF approval requests to exact-content console consent.
// A. Inspect the actual ToolApprovalRequestContent, never assistant prose.
// B. Ask for one human decision only on the matching fixed save operation.
// C. Resume through MAF and independently inspect the saved file.

using System.Text.Json;
using MafClaw.OrchestrationSupport;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample47;

public static class ApprovalConsole
{
    public static async Task<bool> RunAsync(
        AIAgent main, ReportStore store, string prompt, TextReader input, TextWriter output,
        Transcript transcript, CancellationToken cancellationToken = default)
    {
        var session = await main.CreateSessionAsync(cancellationToken);
        var response = await main.RunAsync(prompt, session, cancellationToken: cancellationToken);
        var decisionOffered = false;
        // A response can contain multiple pending calls despite model instructions.
        // At most one may receive console consent; three resumes bound hostile retries.
        for (var round = 0; round < 3; round++)
        {
            output.WriteLine($"MAIN NARRATIVE (not authorization or evidence): {response.Text}");
            var requests = response.Messages.SelectMany(message => message.Contents)
                .OfType<ToolApprovalRequestContent>().ToArray();
            if (requests.Length == 0)
            {
                break;
            }
            var replies = new List<AIContent>();
            foreach (var request in requests)
            {
                var hash = RequestedHash(request);
                var approved = false;
                if (!decisionOffered && hash is not null && hash == store.Proposal?.Sha256)
                {
                    decisionOffered = true;
                    approved = await store.ReviewOnConsoleAsync(hash, input, output, cancellationToken);
                }
                else
                {
                    transcript.Write("Host", "approval-rejected", "Unknown, stale, repeated, or out-of-order approval request.");
                }
                replies.Add(request.CreateResponse(approved, approved
                    ? "Host console approved these exact bytes once."
                    : "Host did not authorize this save."));
            }
            // MAF binds this response to the pending request. Even a fabricated
            // approved response cannot write unless ReportStore was separately armed.
            response = await main.RunAsync([new ChatMessage(ChatRole.User, replies)],
                session, cancellationToken: cancellationToken);
        }
        if (response.Messages.SelectMany(message => message.Contents).OfType<ToolApprovalRequestContent>().Any())
        {
            transcript.Write("Host", "approval-limit", "Pending requests abandoned; no further approval or execution.");
        }
        return store.VerifySavedFile();
    }

    public static string? RequestedHash(ToolApprovalRequestContent request)
    {
        if (request.ToolCall is not FunctionCallContent { Name: "save_report" } call ||
            call.Arguments is null || call.Arguments.Count != 1 ||
            !call.Arguments.TryGetValue("reportHash", out var value))
        {
            return null;
        }
        return value switch
        {
            string text when text.Length == 64 => text,
            JsonElement { ValueKind: JsonValueKind.String } element when element.GetString()?.Length == 64 => element.GetString(),
            _ => null
        };
    }
}
