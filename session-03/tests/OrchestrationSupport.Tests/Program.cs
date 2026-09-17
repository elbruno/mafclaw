// Objective: run offline regression checks for shared orchestration plumbing.
// Steps:
// A. Execute the real MAF pipeline with scripted inference.
// B. Return a failing exit code if any evidence or privacy check fails.

using MafClaw.OrchestrationSupport.Tests;

return await SupportTests.RunAsync();
