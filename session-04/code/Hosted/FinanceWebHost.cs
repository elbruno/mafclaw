// Objective: share the real HTTP composition with the lifecycle regression test.
// A. Register the MAF Responses hosting package.
// B. Expose the hosted capability contract without configuration values.
// C. Leave exporter ownership with the hosting runtime.
using MafClaw.Session04;
using Microsoft.Agents.AI.Foundry.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Text.Json;

namespace MafClaw.Session04.Hosting;

public static class FinanceWebHost
{
    public static WebApplication Create(string[] args, FinanceAgentBuild build, bool fixture)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = 262144);
        builder.Services.ConfigureOpenTelemetryTracerProvider((_, tracing) =>
            tracing.AddProcessor(new RedactingActivityProcessor()));
        builder.Services.ConfigureOpenTelemetryMeterProvider((_, metering) =>
            metering.AddView("*", new MetricStreamConfiguration { TagKeys = RedactingActivityProcessor.AllowedTags }));
        builder.Services.AddFoundryResponses(build.Agent);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (HttpMethods.IsPost(context.Request.Method) &&
                context.Request.Path.Equals("/responses", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.EnableBuffering(bufferThreshold: 65536, bufferLimit: 262144);
                try
                {
                    using var document = await JsonDocument.ParseAsync(context.Request.Body,
                        new JsonDocumentOptions { MaxDepth = 32 }, context.RequestAborted);
                    if (!HostedRequestPolicy.IsAllowed(document.RootElement))
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "Unsupported request. Tools, instructions, model routing and budgets are server-owned."
                        }, context.RequestAborted);
                        return;
                    }
                    context.Request.Body.Position = 0;
                }
                catch (JsonException)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON request." }, context.RequestAborted);
                    return;
                }
            }
            await next(context);
        });
        app.MapFoundryResponses();
        app.MapGet("/capabilities", () => new
        {
            profile = "hosted", inference = fixture ? "fixture" : "live",
            localFileTools = false, shell = false, fileMemory = false, codeAct = false,
            simulatedTrade = false, research = fixture ? "disabled" : "request-scoped"
        });
        return app;
    }
}
