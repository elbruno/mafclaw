using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// A. Represent the research handoff as a MAF function.
// B. The production Harness can replace this with BackgroundAgents.
// C. The ticket remains explicit so the foreground turn can report status.
var researchHandoff = AIFunctionFactory.Create(
    () => "Queued background research for MSFT, NVDA, and SPY.",
    "queue_background_research");

Console.WriteLine("Sample 41 - MAF background-agent bridge");
Console.WriteLine($"Registered function: {researchHandoff.GetType().Name}");
Console.WriteLine(researchHandoff.InvokeAsync(new AIFunctionArguments()).Result);
Console.WriteLine("Production wiring adds BackgroundAgents and durable status tracking.");

static AIAgent ConfigureResearchAgent(IChatClient chatClient)
{
    return chatClient.AsAIAgent(
        name: "TickerResearchAgent",
        description: "Researches recent news for one ticker.",
        instructions: "Return concise, factual findings with sources.",
        tools: []);
}
