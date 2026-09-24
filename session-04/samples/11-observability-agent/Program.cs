// Objective: Sample 11 (MAF/Harness) reveals the agent -> model -> tool -> model execution trace.
// A. Connect to the configured model and observe SDK spans without recording prompt content.
// B. Define one tool and build the Harness visibly in this file.
// C. Run one question; MAF handles dispatch and sends the tool result back to the model.
// D. Print the trace tree and verify the real agent, model and tool relationships.

using System.ComponentModel;
using MafClaw.Session04.Samples;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var fixture = args.Contains("--fixture");
var failModel = args.Contains("--fail-model");
if ((fixture && args.Contains("--live")) || args.Distinct().Count() != args.Length ||
    args.Any(argument => argument is not ("--fixture" or "--live" or "--fail-model")) ||
    (failModel && !fixture))
{
    Console.Error.WriteLine("Run with dotnet run. Automated checks: --fixture [--fail-model].");
    return 2;
}

// A. TraceConsole is just an in-process viewer: the SDK emits the spans, not the viewer.
// Both OpenTelemetry wrappers below omit content. No collector or application factory is required.
using var trace = new TraceConsole();
Console.WriteLine(fixture
    ? "FIXTURE: scripted model responses; REAL MAF Harness and tool execution."
    : "LIVE: configured Foundry model; inference can incur charges.");
if (failModel) Console.WriteLine("FAILURE DEMO: the second model call will deliberately fail after the tool runs.");
var toolCalls = 0;
var completed = false;
var hasToolResult = false;
try
{
    IChatClient model = fixture ? new LessonChatClient(failModel) : LiveModel.Create();

    // Microsoft.Extensions.AI supplies per-model-call spans around either IChatClient implementation.
    using var observedModel = model.AsBuilder().UseOpenTelemetry(sourceName: TraceConsole.SourceName,
        configure: telemetry => telemetry.EnableSensitiveData = false).Build();

    // B. AIFunctionFactory supplies the schema and invocation wrapper; the function stays ordinary C#.
    var tool = AIFunctionFactory.Create(GetLessonTopic, "get_lesson_topic");
    var harness = observedModel.AsHarnessAgent(new HarnessAgentOptions
    {
        Name = "LessonAgent",
        DisableFileMemory = true,
        DisableAgentSkillsProvider = true,
        DisableWebSearch = true,
        DisableTodoProvider = true,
        DisableAgentModeProvider = true,
        DisableToolAutoApproval = true,
        DisableOpenTelemetry = true,
        ChatOptions = new ChatOptions
        {
            Tools = [tool],

            // Keep this tool lesson short; this deployment requires reasoning off for Chat Completions tools.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None },
            Instructions = "Call get_lesson_topic once, then explain its result in one sentence."
        }
    });

    // Microsoft.Agents.AI supplies the outer agent span; Harness supplies function dispatch/history.
    var agent = harness.AsBuilder().UseOpenTelemetry(TraceConsole.SourceName,
        telemetry => telemetry.EnableSensitiveData = false).Build();

    // C. One RunAsync call, not a hand-written model/tool loop. The fixture scripts two model turns.
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    var session = await agent.CreateSessionAsync(deadline.Token);
    var response = await agent.RunAsync("What is this lesson about?", session, cancellationToken: deadline.Token);
    hasToolResult = response.Messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Any();
    Console.WriteLine($"ANSWER: {response.Text}");
    completed = true;
}
catch (Exception exception)
{
    // Keep provider payloads and configuration values off the stream; failure remains a nonzero exit.
    Console.Error.WriteLine($"DEMO FAILURE: {exception.GetType().Name}. See the trace status and setup guide.");
}

Console.WriteLine($"------------------------------------------");
Console.ReadLine();

// D. A fluent-looking answer is not evidence. Require one correlated agent/model/tool trace.
trace.Print();
var passed = trace.HasAgentModelToolTrace() && toolCalls == 1 &&
    (failModel ? !completed && trace.HasError : completed && hasToolResult && !trace.HasError);
Console.WriteLine($"ACTUAL TOOL CALLS: {toolCalls}");
Console.WriteLine(passed ? "OBSERVABILITY AGENT PASS" : "OBSERVABILITY AGENT FAIL: missing or unexpected evidence");

// Exit 1 is the intentional failure demo only when its trace checks passed; broken checks return 2.
return passed ? (failModel ? 1 : 0) : 2;

[Description("Returns the topic of this synthetic workshop lesson without a network call.")]
string GetLessonTopic()
{
    toolCalls++;
    const string topic = "MAF defines the agent; the Harness orchestrates its model and tools.";
    Console.WriteLine($"TOOL RESULT: {topic}");
    return topic;
}
