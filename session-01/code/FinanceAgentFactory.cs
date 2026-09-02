#pragma warning disable OPENAI001
#pragma warning disable MAAI001

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Session01;

internal sealed record HarnessRuntime(
    AIAgent Agent,
    TodoProvider TodoProvider);

internal sealed class FinanceAgentFactory(
    FoundrySettings settings,
    StockTools stockTools,
    Func<TokenCredential>? credentialFactory = null)
{
    // Exposed so tests can assert the default credential type without invoking real auth.
    internal static readonly Func<TokenCredential> DefaultCredentialFactory =
        static () => new AzureCliCredential();

    private readonly FoundrySettings _settings = settings;
    private readonly StockTools _stockTools = stockTools;
    private readonly Func<TokenCredential> _credentialFactory =
        credentialFactory ?? DefaultCredentialFactory;

    // Internal helper so tests can verify credential selection without calling CreateAsync.
    internal TokenCredential BuildCredential() => _credentialFactory();

    public async Task<HarnessRuntime> CreateAsync(CancellationToken cancellationToken)
    {
        var credential = _credentialFactory();
        await EnsureAzureCredentialAsync(credential, cancellationToken);

        IChatClient chatClient = new AIProjectClient(new Uri(_settings.ProjectEndpoint), credential)
            .GetProjectOpenAIClient()
            .GetResponsesClient()
            .AsIChatClient(_settings.Model);

        var agent = chatClient.AsHarnessAgent(BuildOptions(
            new ChatOptions
            {
                Instructions = BuildInstructions(),
                Tools = [_stockTools.CreateGetStockPriceTool()]
            }));

        var todoProvider = agent.GetService<TodoProvider>() ??
                           throw new InvalidOperationException("Harness todo provider is unavailable.");

        return new HarnessRuntime(agent, todoProvider);
    }

    private static async Task EnsureAzureCredentialAsync(
        TokenCredential credential,
        CancellationToken cancellationToken)
    {
        await credential.GetTokenAsync(
            new TokenRequestContext(["https://ai.azure.com/.default"]),
            cancellationToken);
    }

    internal static HarnessAgentOptions BuildOptions(ChatOptions chatOptions) =>
        new()
        {
            Name = "mafclaw-session-01",
            DisableFileMemory = true,
            DisableAgentModeProvider = true,
            ChatOptions = chatOptions
        };

    private static string BuildInstructions()
    {
        return
            """
            ## Personal Finance Assistant Instructions

            You are a personal finance and investing assistant.
            - Use get_stock_price for stock numbers.
            - Use hosted web search for recent market context when the service supports it.
            - Cite web sources inline when you use web search.
            - Keep responses concise and educational.
            - Never provide personalized financial advice.
            """;
    }
}
