// Objective: Sample 41 (MAF) exposes a tiny agent through the SDK's Responses HTTP protocol.
// A. Build an explicit fixture or live generic agent.
// B. Register AddFoundryResponses and MapFoundryResponses directly in this file.
// C. Verify a real completed response over loopback, not merely an HTTP 200.

using System.Net.Http.Json;
using System.Text.Json;
using MafClaw.Samples;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

try
{
    var fixture = args.Contains("--fixture");
    var selfTest = args.Contains("--self-test");
    if (args.Any(argument => argument is not ("--fixture" or "--self-test")) || (selfTest && !fixture))
        throw new InvalidOperationException("Usage: [--fixture] [--self-test]; self-test requires fixture.");

    // A. The hosting runtime owns conversation storage; do not add a second Harness history owner.
    using IChatClient model = fixture
        ? new FixtureChatClient(FixtureChatClient.Text("Hello from the hosted MAF agent."))
        : DemoSettings.Load().CreateChatClient();
    var agent = model.AsAIAgent(new ChatClientAgentOptions
    {
        Name = "HostedLessonAgent",
        UseProvidedChatClientAsIs = true,
        ChatOptions = new ChatOptions
        {
            Instructions = "You are a small workshop greeting assistant. Reply in one sentence.",
            MaxOutputTokens = 200,
            RawRepresentationFactory = _ => new ChatCompletionOptions { StoredOutputEnabled = false }
        }
    });

    // B. Microsoft.Agents.AI.Foundry.Hosting owns the wire protocol, routing and serialization.
    // Unlike Sample 40, we do not hand-write a route that turns a request into an agent call.
    var builder = WebApplication.CreateBuilder();
    builder.Services.AddFoundryResponses(agent);
    await using var app = builder.Build();
    app.MapFoundryResponses();
    app.Urls.Clear();
    app.Urls.Add(selfTest ? "http://127.0.0.1:0" : "http://127.0.0.1:5091");
    Console.WriteLine("LOOPBACK TEACHING HOST: no authentication, no tools, no cloud deployment.");
    if (!selfTest) { await app.RunAsync(); return 0; }

    // C. A real request must return status=completed and the expected fixture answer.
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await app.StartAsync(deadline.Token);
    using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    using var response = await http.PostAsJsonAsync("/responses",
        new { model = "HostedLessonAgent", input = "Say hello to the workshop." }, deadline.Token);
    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
    var passed = response.IsSuccessStatusCode &&
        document.RootElement.GetProperty("status").GetString() == "completed" &&
        document.RootElement.GetRawText().Contains("Hello from the hosted MAF agent.");
    await app.StopAsync(deadline.Token);
    Console.WriteLine(passed ? "HOSTED AGENT PASS: local fixture, not cloud deployment." : "HOSTED AGENT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception) { return DemoOutput.Report(exception); }
