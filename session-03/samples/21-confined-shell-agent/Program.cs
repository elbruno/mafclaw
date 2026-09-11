using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// A. Wrap the host-owned shell policy as a MAF function.
// B. Keep approval and process limits outside the model.
// C. Attach the function to AsHarnessAgent in the live advisor.
var shellCheck = AIFunctionFactory.Create(
    () => "Allowlisted shell result: dotnet --version completed.",
    "workspace_scan");

Console.WriteLine("Sample 21 - MAF confined-shell bridge");
Console.WriteLine($"Registered function: {shellCheck.GetType().Name}");
Console.WriteLine(shellCheck.InvokeAsync(new AIFunctionArguments()).Result);
Console.WriteLine("Policy remains application-owned: allowlist, cwd, timeout, output cap.");

static AIAgent ConfigureHarness(IChatClient chatClient, AIFunction shell)
{
    return chatClient.AsHarnessAgent(new HarnessAgentOptions
    {
        ChatOptions = new ChatOptions { Tools = [shell] }
    });
}
