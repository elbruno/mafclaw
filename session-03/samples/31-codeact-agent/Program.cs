using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// A. Expose the auditable calculator as a MAF function.
// B. The host owns the implementation and input validation.
// C. A live agent can request this function through its tool surface.
var calculator = AIFunctionFactory.Create(
    () => "Mock CodeAct result: MSFT 35x430.12 + NVDA 20x142.50 = 17,453.20 USD.",
    "calculate_portfolio_value");

Console.WriteLine("Sample 31 - MAF CodeAct bridge");
Console.WriteLine($"Registered function: {calculator.GetType().Name}");
Console.WriteLine(calculator.InvokeAsync(new AIFunctionArguments()).Result);
Console.WriteLine("The result keeps the calculation auditable and separate from prose.");

static AIAgent ConfigureHarness(IChatClient chatClient, AIFunction calculator)
{
    return chatClient.AsHarnessAgent(new HarnessAgentOptions
    {
        ChatOptions = new ChatOptions { Tools = [calculator] }
    });
}
