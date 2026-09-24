// Objective: share chat connection settings across generic teaching samples, not an agent definition.
// A. Read the existing Session 4 user-secrets/environment keys.
// B. Validate chat settings without logging their values.
// C. Adapt Foundry Chat Completions to IChatClient; each sample owns its agent and conversation history.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace MafClaw.Samples;

public sealed record DemoSettings(Uri ProjectEndpoint, string Model)
{
    // A. Optional service samples can read their own extra keys from the same store.
    public static IConfigurationRoot ReadConfiguration() => new ConfigurationBuilder()
        .AddUserSecrets<DemoSettings>(optional: true).AddEnvironmentVariables().Build();

    public static DemoSettings Load()
    {
        var settings = ReadConfiguration();
        var endpoint = settings["Foundry:ProjectEndpoint"] ?? settings["FOUNDRY_PROJECT_ENDPOINT"];
        var model = settings["Foundry:Model"] ?? settings["FOUNDRY_MODEL"];

        // B. No implicit fixture fallback: a live run requires explicit valid configuration.
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(model))
        {
            Console.Error.WriteLine("Configure Foundry:ProjectEndpoint and Foundry:Model with tools\\configure-user-secrets.ps1 -Session 4.");
            throw new InvalidOperationException("Missing or invalid chat configuration.");
        }
        return new(uri, model);
    }

    // C. This helper creates only a model client. Tools, instructions, Harness and policy stay in Program.cs.
    public IChatClient CreateChatClient() => new AIProjectClient(ProjectEndpoint, new AzureCliCredential())
        .GetProjectOpenAIClient().GetChatClient(Model).AsIChatClient();
}
