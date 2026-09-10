// Session flow:
// A. Describe the simulated trade as a side effect.
// B. Ask the human for an explicit yes/no decision.
// C. Execute the demo action only after approval.
// D. Print the safe denied path when approval is refused.

Console.WriteLine("Session 2 sample 20: approval gates");
Console.WriteLine("A trade is a side effect and should require human approval.");

// Ask for consent before the simulated side effect can happen.
var approved = Confirm("Approve this simulated buy: 10 shares of MSFT?");
if (approved)
{
    Console.WriteLine("Approved: trade request sent to the order service.");
}
else
{
    Console.WriteLine("Denied: no trade executed.");
}

static bool Confirm(string prompt)
{
    // Keep the approval prompt explicit and default to no.
    Console.Write($"{prompt} [y/N]: ");
    var response = Console.ReadLine();
    return response is not null && (response.Equals("y", StringComparison.OrdinalIgnoreCase) || response.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
