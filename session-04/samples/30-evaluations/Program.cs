// Objective: make an evaluation gate visible before introducing MAF.
// A. Calculate the fixed mock portfolio.
// B. Optionally inject a deliberate numerical regression.
// C. Fail on an incorrect value, not merely on missing digits.
using MafClaw.Session04;

if (args.Length > 1 || (args.Length == 1 && args[0] != "--inject-regression"))
{
    Console.Error.WriteLine("Usage: [--inject-regression]");
    return 2;
}
var result = MockPortfolio.Summarize();
var observedTotal = args.Contains("--inject-regression") ? 1m : result.Total;
var passed = observedTotal == 27124.95m && result.TechnologyPercent == 66.01m;
Console.WriteLine($"Observed total: {observedTotal:F2}; Technology: {result.TechnologyPercent:F2}%.");
Console.WriteLine(passed ? "EVALUATION PRIMITIVE PASS" : "EVALUATION PRIMITIVE FAIL");
return passed ? 0 : 1;
