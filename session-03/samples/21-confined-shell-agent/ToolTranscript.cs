// Objective: separate proposed commands, approval decisions and actual SDK results.
// Steps:
// A. Display the exact PowerShell command before asking for permission.
// B. Keep a decision per call ID; denial never means execution.
// C. Print FunctionResultContent once, separately from the assistant's narration.

using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample21;

internal sealed class ToolTranscript(TextWriter output)
{
    private readonly Dictionary<string, (int Number, bool Approved)> _decisions = [];
    private readonly HashSet<string> _reported = [];
    private int _nextNumber = 1;

    public (AIContent Response, bool EndOfInput) AskApproval(
        ToolApprovalRequestContent request, TextReader input)
    {
        // A. Keep the full command visible; do not approve a model-generated description.
        var number = _nextNumber++;
        if (request.ToolCall is not FunctionCallContent { Name: "run_shell" } call ||
            !TryGetCommand(call, out var command))
        {
            output.WriteLine($"APPROVAL #{number}: DENIED (unrecognized shell request).");
            _decisions[request.ToolCall.CallId] = (number, false);
            return (request.CreateResponse(false, "Unrecognized shell request."), false);
        }
        output.WriteLine($"\nPROPOSED COMMAND #{number} (PowerShell):");
        output.WriteLine(command);
        output.Write($"Approve command #{number}? [y/N]: ");
        var answer = input.ReadLine();
        var approved = answer?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true ||
                       answer?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) == true;

        // B. MAF binds this response to the pending request; we do not invoke tools here.
        _decisions[call.CallId] = (number, approved);
        output.WriteLine(approved
            ? $"APPROVAL #{number}: APPROVED."
            : $"APPROVAL #{number}: DENIED - command not executed.");
        return (request.CreateResponse(approved,
            approved ? "Approved by console user." : "Denied by console user."), answer is null);
    }

    public void WriteResults(AgentResponse response)
    {
        // C. These are executor results carried by MAF, not sentences from response.Text.
        foreach (var result in response.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>())
        {
            if (!_reported.Add(result.CallId))
            {
                continue;
            }
            if (!_decisions.TryGetValue(result.CallId, out var decision))
            {
                throw new InvalidOperationException("A tool result arrived without a recorded approval decision.");
            }
            output.WriteLine($"\nTOOL RESULT #{decision.Number}:");
            if (!decision.Approved)
            {
                output.WriteLine("  Not executed: the command was denied.");
                continue;
            }
            if (result.Exception is not null)
            {
                output.WriteLine($"  Tool failed ({result.Exception.GetType().Name}).");
            }
            var text = result.Result switch
            {
                string value => value,
                JsonElement { ValueKind: JsonValueKind.String } value => value.GetString(),
                JsonElement value => value.GetRawText(),
                null => null,
                var value => JsonSerializer.Serialize(value)
            };
            output.WriteLine(text ?? "No result supplied; execution is not confirmed.");
        }
    }

    private static bool TryGetCommand(FunctionCallContent call, out string command)
    {
        command = string.Empty;
        if (call.Arguments?.TryGetValue("command", out var value) != true)
        {
            return false;
        }
        command = value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString() ?? string.Empty,
            _ => string.Empty
        };
        return !string.IsNullOrWhiteSpace(command);
    }
}
