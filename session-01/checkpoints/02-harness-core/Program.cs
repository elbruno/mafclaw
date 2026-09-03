#pragma warning disable OPENAI001
#pragma warning disable MAAI001

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

return await RunAsync();

static async Task<int> RunAsync()
{
    const string instructions =
        """
        You are a personal finance education assistant.
        Keep responses concise and educational.
        Never provide personalized financial advice.
        """;

    const string prompt =
        "What are three things a beginner should consider before buying a stock?";

    try
    {
        var settings = FoundryConfiguration.Resolve();
        var projectClient = new AIProjectClient(
            new Uri(settings.ProjectEndpoint),
            new AzureCliCredential());

        IChatClient chatClient = projectClient
            .GetProjectOpenAIClient()
            .GetResponsesClient()
            .AsIChatClient(settings.Model);

        AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
        {
            Name = "mafclaw-harness-core",
            HarnessInstructions = string.Empty,
            DisableCompaction = true,
            DisableFileMemory = true,
            DisableWebSearch = true,
            DisableTodoProvider = true,
            DisableAgentModeProvider = true,
            DisableAgentSkillsProvider = true,
            DisableOpenTelemetry = true,
            DisableToolAutoApproval = true,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions
            }
        });

        Console.WriteLine("LIVE · checkpoint 02 · harness core");
        Console.WriteLine(await agent.RunAsync(prompt));
        return 0;
    }
    catch (Exception exception)
    {
        return SafeErrors.Write(exception);
    }
}
