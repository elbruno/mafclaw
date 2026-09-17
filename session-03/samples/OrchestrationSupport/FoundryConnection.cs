// Objective: share the existing Session 3 Foundry connection, not sample orchestration.
// Steps:
// A. Read the Session 3 user-secrets and environment aliases privately.
// B. Validate the required endpoint without printing its value.
// C. Adapt the Foundry Responses client to MAF's IChatClient abstraction.

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace MafClaw.OrchestrationSupport;

public static class FoundryConnection
{
    public static IChatClient CreateClient()
    {
        // A. These are the same settings used by Sample 41; no new Azure resource is required.
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("7a3d9e21-58f4-4c6b-9e02-1a6f4d8c5e73")
            .AddEnvironmentVariables()
            .Build();
        var endpoint = configuration["Foundry:ProjectEndpoint"] ?? configuration["FOUNDRY_PROJECT_ENDPOINT"];
        var model = configuration["Foundry:Model"] ?? configuration["FOUNDRY_MODEL"] ?? "gpt-5-mini";

        // B. Report the missing key, never an endpoint, account identifier, or credential.
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new SampleConfigurationException(
                @"Missing Foundry:ProjectEndpoint. Configure Session 3 with .\tools\configure-user-secrets.ps1 -Session 3, or explicitly select --mode fixture.");
        }
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(model))
        {
            throw new SampleConfigurationException("Foundry settings are invalid. Check the HTTPS endpoint and model privately.");
        }

        // C. Azure.AI.Projects and the MAF Foundry extensions supply the protocol adapter.
        return new AIProjectClient(uri, new AzureCliCredential())
            .GetProjectOpenAIClient()
            .GetResponsesClient()
            .AsIChatClient(model);
    }
}
