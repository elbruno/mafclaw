// Objective: Sample 20 (plain C#) enforces a rule before a mock action, without an agent.
// A. Prepare one ordinary note and one synthetic restricted note.
// B. Check the rule before adding anything to the in-memory outbox.
// C. Verify that the denied note never reached the outbox.

// A. Nothing is sent anywhere. This small list makes the side effect visible to the audience.
var outbox = new List<string>();
var requests = new[] { "Welcome to the workshop.", "PRIVATE-DEMO: do not publish this synthetic note." };

// B. The application owns this rule; it is not a system prompt or enterprise DLP product.
foreach (var note in requests)
{
    if (note.Contains("PRIVATE-DEMO", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("DENIED: synthetic restricted marker; no action.");
        continue;
    }

    outbox.Add(note);
    Console.WriteLine("ALLOWED: added one mock note to the local outbox.");
}

// C. Inspect state, not just a denial message. Sample 21 adds a real MAF approval pause.
Console.WriteLine($"OUTBOX COUNT: {outbox.Count}. No message was sent.");
var passed = outbox.Count == 1 && outbox[0] == requests[0];
Console.WriteLine(passed ? "GOVERNANCE PRIMITIVE PASS" : "GOVERNANCE PRIMITIVE FAIL");
return passed ? 0 : 1;
