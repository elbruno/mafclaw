// Objective: Sample 31 (MAF/Harness) grades an answer and a real tool call, not just fluent prose.
// A. Build a tiny calculator agent directly with one add_numbers tool.
// B. Define MAF FunctionEvaluator checks and combine them with LocalEvaluator.
// C. Run EvaluateAsync and show whether the gate accepted or rejected the conversation.

using System.ComponentModel;
using System.Text.Json;
using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

try
{
    if (args.Length > 0 && (args.Length > 2 || args[0] is not ("--fixture" or "--live") ||
        (args.Length == 2 && (args[0] != "--fixture" || args[1] != "--inject-regression"))))
        throw new InvalidOperationException("Run with dotnet run. Automated checks: --fixture [--inject-regression].");
    var fixture = args.Contains("--fixture");

    // A. Script inference only; the Harness still invokes the ordinary C# tool.
    [Description("Add two small integers for an educational calculator example.")]
    static int AddNumbers(int a, int b) => checked(a + b);
    using IChatClient model = fixture
        ? new FixtureChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("addition", "add_numbers",
                    new Dictionary<string, object?> { ["a"] = 2, ["b"] = 3 })])),
            FixtureChatClient.Text(args.Contains("--inject-regression") ? "99" : "5"))
        : DemoSettings.Load().CreateChatClient();
    var agent = model.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "CalculatorDemo",
        DisableFileMemory = true, DisableAgentSkillsProvider = true, DisableWebSearch = true,
        DisableTodoProvider = true, DisableAgentModeProvider = true, DisableOpenTelemetry = true,
        DisableToolAutoApproval = true,
        ChatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(AddNumbers, "add_numbers")],

            // Keep this tool lesson short; this deployment requires reasoning off for Chat Completions tools.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None },
            Instructions = "Use add_numbers for arithmetic. Reply with only the integer result."
        }
    });

    // B. Microsoft.Agents.AI owns evaluation execution/aggregation. Our predicates define success.
    // A correct-looking answer is insufficient: the conversation must also contain the tool's result.
    var evaluator = new LocalEvaluator(
        FunctionEvaluator.Create("correct_answer", (EvalItem item) => item.Response.Trim() == "5"),
        FunctionEvaluator.Create("actual_tool_result", (EvalItem item) =>
            item.Conversation.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
                .Any(result => JsonSerializer.SerializeToElement(result.Result).ToString() == "5")));

    // C. Live inference can cost money even though these checks run locally.
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    var results = await agent.EvaluateAsync(["Use add_numbers to calculate 2 + 3."], evaluator,
        cancellationToken: deadline.Token);
    Console.WriteLine($"Inference: {(fixture ? "fixture" : "LIVE")}; grading: local MAF.");
    Console.WriteLine($"{results.Passed}/{results.Total} cases passed.");
    Console.WriteLine(results.AllPassed ? "EVALUATION AGENT PASS" : "EVALUATION AGENT FAIL");
    return results.AllPassed ? 0 : 1;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
