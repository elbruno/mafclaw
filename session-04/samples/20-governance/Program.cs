// Objective: show application policy before a side effect in plain C#.
// A. Supply one allowed and one synthetically restricted request.
// B. Enforce policy before changing state.
// C. Verify that only the allowed request changed state.
using MafClaw.Session04;

var actions = 0;
foreach (var request in new[] { "Explain the mock portfolio.", FinancePolicy.RestrictedMarker })
{
    var rule = FinancePolicy.GetBlockingRule(request);
    if (rule is not null) { Console.WriteLine($"DENIED: {rule}; no action."); continue; }
    actions++;
    Console.WriteLine("ALLOWED: recorded a mock local action.");
}
Console.WriteLine("This is a teaching rule, not Purview or a production DLP system.");
Console.WriteLine(actions == 1 ? "GOVERNANCE PRIMITIVE PASS" : "GOVERNANCE PRIMITIVE FAIL");
return actions == 1 ? 0 : 1;
