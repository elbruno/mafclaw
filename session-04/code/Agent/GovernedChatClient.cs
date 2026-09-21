// Objective: apply the same visible application rule to every model client.
// A. Block restricted input before calling inference.
// B. Screen the completed output before returning it.
// C. Buffer streaming responses so blocked text is never partially displayed.
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MafClaw.Session04;

public sealed class GovernedChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var input = messages.ToArray();
        var rule = input.Select(message => FinancePolicy.GetBlockingRule(GetScreeningText(message)))
            .FirstOrDefault(value => value is not null);
        if (rule is not null) return Block(rule);
        var response = await base.GetResponseAsync(input, options, cancellationToken);
        rule = response.Messages.Select(message => FinancePolicy.GetBlockingRule(GetScreeningText(message)))
            .FirstOrDefault(value => value is not null);
        return rule is null ? response : Block(rule);
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates()) yield return update;
    }

    private static ChatResponse Block(string rule)
    {
        FinanceTelemetry.PolicyDecisions.Add(1, new KeyValuePair<string, object?>("policy.rule", rule));
        return new(new ChatMessage(ChatRole.Assistant, FinancePolicy.BlockMessage));
    }

    private static string GetScreeningText(ChatMessage message) => message.Text + "\n" +
        string.Join('\n', message.Contents.Select(content => content switch
        {
            FunctionCallContent call => JsonSerializer.Serialize(call.Arguments),
            FunctionResultContent result => JsonSerializer.Serialize(result.Result),
            _ => ""
        }));
}
