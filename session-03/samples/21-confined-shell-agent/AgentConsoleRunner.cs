// Objective: tell the inspect/propose/approve/execute/verify story without hiding evidence.
// Steps:
// A. Create one MAF session and accept a prompt or a local /verify command.
// B. Display real tool results and request approval for every proposed shell call.
// C. Resume through MAF, then independently verify the files when the turn finishes.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Sample21;

internal static class AgentConsoleRunner
{
    public static async Task RunAsync(
        AIAgent agent, DemoWorkspace workspace, TextReader? input = null, TextWriter? output = null)
    {
        input ??= Console.In;
        output ??= Console.Out;
        var transcript = new ToolTranscript(output);
        // A. MAF owns conversation history; the host owns local verification.
        var session = await agent.CreateSessionAsync();
        while (true)
        {
            output.Write("\n> ");
            var prompt = input.ReadLine()?.Trim();
            if (prompt is null || prompt.Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (prompt.Equals("/verify", StringComparison.OrdinalIgnoreCase))
            {
                DemoVerifier.Verify(workspace, output);
                continue;
            }
            if (prompt.StartsWith('/'))
            {
                output.WriteLine("Unknown console command. Use /verify or /exit; mode switching is not part of this sample.");
                continue;
            }
            if (prompt.Length == 0)
            {
                continue;
            }

            var response = await agent.RunAsync(prompt, session);
            while (true)
            {
                // B. Results come from FunctionResultContent, not the assistant's claims.
                transcript.WriteResults(response);
                if (!string.IsNullOrWhiteSpace(response.Text))
                {
                    output.WriteLine($"\nASSISTANT: {response.Text}");
                }
                var requests = response.Messages.SelectMany(message => message.Contents)
                    .OfType<ToolApprovalRequestContent>().ToList();
                if (requests.Count == 0)
                {
                    break;
                }

                var approvals = new List<AIContent>();
                foreach (var request in requests)
                {
                    var decision = transcript.AskApproval(request, input);
                    if (decision.EndOfInput)
                    {
                        output.WriteLine("Input closed. Pending commands were not submitted for execution.");
                        DemoVerifier.Verify(workspace, output);
                        return;
                    }
                    approvals.Add(decision.Response);
                }

                // C. AsHarnessAgent handles approval binding and actual tool invocation.
                response = await agent.RunAsync([new ChatMessage(ChatRole.User, approvals)], session);
            }
            DemoVerifier.Verify(workspace, output);
        }
    }
}
