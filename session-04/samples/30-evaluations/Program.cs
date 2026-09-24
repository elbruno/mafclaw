// Objective: Sample 30 (plain C#) separates a candidate answer from the rule that grades it.
// A. Produce a tiny deterministic answer: 2 + 3.
// B. Optionally replace it with a deliberate regression.
// C. Compare against an independent expected value and return the gate's exit code.

if (args.Length > 1 || (args.Length == 1 && args[0] != "--inject-regression"))
{
    Console.Error.WriteLine("Usage: [--inject-regression]");
    return 2;
}

// A. Keep the task trivial so the lesson is about the evaluation gate, not business arithmetic.
var answer = 2 + 3;

// B. A successful execution can still produce a bad answer.
var candidate = args.Contains("--inject-regression") ? 99 : answer;

// C. Run good/bad/good on stream: the process exits should be 0/1/0.
var passed = candidate == 5;
Console.WriteLine($"Question: 2 + 3. Candidate: {candidate}; expected: 5.");
Console.WriteLine(passed ? "EVALUATION PRIMITIVE PASS" : "EVALUATION PRIMITIVE FAIL");
return passed ? 0 : 1;
