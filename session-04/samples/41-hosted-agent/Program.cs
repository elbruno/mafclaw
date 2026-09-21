// Objective: bridge the HTTP contract to MAF's Foundry Responses hosting.
// A. Select an explicit fixture or a configured live model.
// B. Start the real shared Responses host with restricted capabilities.
// C. In self-test mode, require a completed response, not just HTTP 200.
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Identity;
using MafClaw.Session04;
using MafClaw.Session04.Hosting;

try
{
    var fixture = args.Contains("--fixture");
    var selfTest = args.Contains("--self-test");
    if (selfTest && !fixture) throw new FinanceConfigurationException("--self-test requires --fixture; it never silently calls a model.");
    var settings = fixture ? null : FinanceSettings.Load();
    await using var build = await FinanceAgentFactory.CreateAsync(new FinanceAgentOptions
    {
        Profile = FinanceHostProfile.Hosted, Settings = settings,
        ChatClient = fixture ? ScriptedChatClient.Portfolio() : null,
        Credential = new AzureCliCredential(), EnableResearch = false
    });
    await using var app = FinanceWebHost.Create(
        args.Where(argument => argument is not ("--fixture" or "--self-test")).ToArray(), build, fixture);
    if (!selfTest) { await app.RunAsync(); return 0; }
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    await app.StartAsync(deadline.Token);
    using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    using var response = await http.PostAsJsonAsync("/responses",
        new { model = "MafClawFinance", input = FinanceEvaluations.Query }, deadline.Token);
    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
    var passed = response.IsSuccessStatusCode &&
        document.RootElement.GetProperty("status").GetString() == "completed" &&
        document.RootElement.GetRawText().Contains("27124.95");
    await app.StopAsync(deadline.Token);
    Console.WriteLine(passed ? "HOSTED AGENT PASS: local fixture, not cloud deployment." : "HOSTED AGENT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception) { return SafeErrors.Report(exception); }
