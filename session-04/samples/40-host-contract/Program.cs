// Objective: teach a real HTTP lifecycle using plain C# and ASP.NET.
// A. Expose readiness and a synthetic read-only portfolio endpoint.
// B. Optionally start on a free loopback port and make a real request.
// C. Verify the response and stop the server.
using System.Net.Http.Json;
using MafClaw.Session04;

try
{
    var selfTest = args.Contains("--self-test");
    var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--self-test").ToArray());
    await using var app = builder.Build();
    app.MapGet("/readiness", () => Results.Ok(new { ready = true }));
    app.MapGet("/portfolio", () => MockPortfolio.Summarize());
    if (!selfTest) { await app.RunAsync(); return 0; }
    app.Urls.Clear();
    app.Urls.Add("http://127.0.0.1:0");
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await app.StartAsync(deadline.Token);
    using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    var result = await http.GetFromJsonAsync<PortfolioSummary>("/portfolio", deadline.Token);
    await app.StopAsync(deadline.Token);
    var passed = result?.Total == 27124.95m;
    Console.WriteLine(passed ? "HOST CONTRACT PASS" : "HOST CONTRACT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Host contract failed: {exception.GetType().Name}");
    return 1;
}
