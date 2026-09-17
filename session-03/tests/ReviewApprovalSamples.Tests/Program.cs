// Objective: execute focused offline checks using actual MAF orchestration.
// A. Run ordered collaboration checks.
// B. Run human approval and file-evidence checks.
// C. Return a nonzero exit code if any assertion fails.

using MafClaw.ReviewApprovalSamples.Tests;

return await TestRunner.RunAsync();
