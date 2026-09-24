// Objective: isolate optional Foundry configuration from Sample 11's Harness walkthrough.
// A. Load the existing Session 4 chat settings without displaying their values.
// B. Validate the endpoint/model and refuse content capture.
// C. Adapt project Chat Completions to IChatClient; the Harness owns session history.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace MafClaw.Session04.Samples;

public static class LiveModel
{
    public static IChatClient Create()
    {
        // A. This project retains UserSecretsId mafclaw-session-04; the existing setup script still applies.
        var settings = new ConfigurationBuilder()
            .AddUserSecrets(typeof(LiveModel).Assembly, optional: true).AddEnvironmentVariables().Build();
        var endpoint = settings["Foundry:ProjectEndpoint"] ?? settings["FOUNDRY_PROJECT_ENDPOINT"];
        var model = settings["Foundry:Model"] ?? settings["FOUNDRY_MODEL"];

        // B. Missing configuration is an error, never an implicit switch to fixture inference.
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Configure Foundry:ProjectEndpoint and Foundry:Model with the Session 4 setup script.");
        if (string.Equals(settings["OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT"], "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Message-content telemetry is disabled for this teaching sample.");

        // C. Azure.AI.Projects + Microsoft.Extensions.AI.OpenAI supply the bridge into MAF.
        return new AIProjectClient(uri, new AzureCliCredential())
            .GetProjectOpenAIClient().GetChatClient(model).AsIChatClient();
    }
}
