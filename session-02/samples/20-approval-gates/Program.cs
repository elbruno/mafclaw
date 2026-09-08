Console.WriteLine("Session 2 sample 20: approval gates");
Console.WriteLine("A trade is a side effect and should require human approval.");

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
    Console.Write($"{prompt} [y/N]: ");
    var response = Console.ReadLine();
    return response is not null && (response.Equals("y", StringComparison.OrdinalIgnoreCase) || response.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
