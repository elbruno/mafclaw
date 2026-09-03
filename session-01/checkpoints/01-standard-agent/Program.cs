#pragma warning disable MAAI001

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

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

        AIAgent agent = new AIProjectClient(
                new Uri(settings.ProjectEndpoint),
                new AzureCliCredential())
            .AsAIAgent(
                model: settings.Model,
                instructions: instructions,
                name: "mafclaw-standard-agent");

        Console.WriteLine("LIVE · checkpoint 01 · standard agent");
        Console.WriteLine(await agent.RunAsync(prompt));
        return 0;
    }
    catch (Exception exception)
    {
        return SafeErrors.Write(exception);
    }
}
