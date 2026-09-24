// Objective: show the HTTP boundary shared by the complete hosted app and Sample 41.
// A. Configure request size, privacy filtering and the MAF Responses service.
// B. Reject client attempts to replace server-owned tools, instructions or budgets.
// C. Map the protocol and a non-sensitive capability summary.

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
        // A. Keep operational limits and telemetry policy at the host boundary, outside model prompts.
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = 262144);
        builder.Services.ConfigureOpenTelemetryTracerProvider((_, tracing) =>
            tracing.AddProcessor(new RedactingActivityProcessor()));
        builder.Services.ConfigureOpenTelemetryMeterProvider((_, metering) =>
            metering.AddView("*", new MetricStreamConfiguration { TagKeys = RedactingActivityProcessor.AllowedTags }));

        // Microsoft.Agents.AI.Foundry.Hosting registers protocol handling for the supplied agent.
        // Hosting owns exporter lifetimes; do not add a second FinanceTelemetry pipeline here.
        builder.Services.AddFoundryResponses(build.Agent);
        var app = builder.Build();

        // B. Inspect the JSON before it reaches the SDK; clients may converse, not grant capabilities.
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

                    // Rewind after inspection so the SDK receives the same request body.
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

        // C. The SDK maps /responses; our extra route explains what this hosted profile cannot do.
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
