// Objective: Sample 40 (plain C#/ASP.NET) introduces an HTTP lifecycle before agent hosting.
// A. Map two tiny read-only endpoints.
// B. Start a loopback server and optionally make a real self-test request.
// C. Inspect the JSON response, stop the server and report the result.

using System.Net.Http.Json;
using System.Text.Json;

try
{
    if (args.Length > 1 || (args.Length == 1 && args[0] != "--self-test"))
        throw new InvalidOperationException("Usage: [--self-test]");
    var selfTest = args.Contains("--self-test");

    // A. These are ordinary HTTP routes: no model, agent or SDK protocol is involved yet.
    var builder = WebApplication.CreateBuilder();
    await using var app = builder.Build();
    app.MapGet("/readiness", () => Results.Ok(new { ready = true }));
    app.MapGet("/message", () => new { message = "Hello from plain HTTP." });

    // B. This unauthenticated teaching server stays on loopback; it is not a public deployment.
    app.Urls.Clear();
    app.Urls.Add(selfTest ? "http://127.0.0.1:0" : "http://127.0.0.1:5090");
    if (!selfTest) { await app.RunAsync(); return 0; }
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    await app.StartAsync(deadline.Token);
    using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
    var result = await http.GetFromJsonAsync<JsonElement>("/message", deadline.Token);

    // C. A listening port is not enough: require the expected payload from the real HTTP call.
    var passed = result.GetProperty("message").GetString() == "Hello from plain HTTP.";
    await app.StopAsync(deadline.Token);
    Console.WriteLine(passed ? "HOST CONTRACT PASS" : "HOST CONTRACT FAIL");
    return passed ? 0 : 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Host contract failed: {exception.GetType().Name}");
    return 1;
}
