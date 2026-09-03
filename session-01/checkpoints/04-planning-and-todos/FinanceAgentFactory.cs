#pragma warning disable OPENAI001
#pragma warning disable MAAI001

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafClaw.Checkpoint04;

internal sealed record HarnessRuntime(
    AIAgent Agent,
    TodoProvider TodoProvider);

internal sealed class FinanceAgentFactory(
    FoundrySettings settings,
    StockTools stockTools)
{
    private readonly FoundrySettings _settings = settings;
    private readonly StockTools _stockTools = stockTools;

    public async Task<HarnessRuntime> CreateAsync(CancellationToken cancellationToken)
    {
        var credential = new AzureCliCredential();
        await credential.GetTokenAsync(
            new TokenRequestContext(["https://ai.azure.com/.default"]),
            cancellationToken);

        IChatClient chatClient = new AIProjectClient(
                new Uri(_settings.ProjectEndpoint),
                credential)
            .GetProjectOpenAIClient()
            .GetResponsesClient()
            .AsIChatClient(_settings.Model);

        var agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
        {
            Name = "mafclaw-planning-and-todos",
            DisableCompaction = true,
            DisableFileMemory = true,
            DisableAgentModeProvider = true,
            DisableAgentSkillsProvider = true,
            DisableOpenTelemetry = true,
            DisableToolAutoApproval = true,
            DisableWebSearch = false,
            DisableTodoProvider = false,
            ChatOptions = new ChatOptions
            {
                Instructions =
                    """
                    ## Personal Finance Assistant Instructions

                    You are a personal finance and investing education assistant.
                    - Use get_stock_price for stock numbers.
                    - Use hosted web search for recent market context.
                    - Cite web sources inline when you use web search.
                    - Use the Harness todo list for multi-step work.
                    - Keep responses concise and educational.
                    - Never provide personalized financial advice.
                    """,
                Tools = [_stockTools.CreateGetStockPriceTool()]
            }
        });

        var todoProvider = agent.GetService<TodoProvider>() ??
                           throw new InvalidOperationException(
                               "The Harness todo provider is unavailable.");

        return new HarnessRuntime(agent, todoProvider);
    }
}
