using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// A. Create the same capability as an Agent Framework function.
// B. Keep the catalog/host boundary visible before adding a model.
// C. The complete app can attach this function to AsHarnessAgent.
var portfolioCheck = AIFunctionFactory.Create(
    () => "Mock watchlist: MSFT, NVDA, SPY.",
    "portfolio_check");

Console.WriteLine("Sample 11 - MAF skill bridge");
Console.WriteLine($"Registered function: {portfolioCheck.GetType().Name}");
Console.WriteLine(portfolioCheck.InvokeAsync(new AIFunctionArguments()).Result);
Console.WriteLine("Offline bridge: no model call or credentials required.");

static AIAgent ConfigureHarness(IChatClient chatClient, AIFunction skill)
{
    return chatClient.AsHarnessAgent(new HarnessAgentOptions
    {
        ChatOptions = new ChatOptions { Tools = [skill] }
    });
}
