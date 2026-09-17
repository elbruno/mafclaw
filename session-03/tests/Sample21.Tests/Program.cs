// Objective: run Sample 21's local regression checks without cloud credentials.
// Steps:
// A. Use real MAF and PowerShell with deterministic fake model responses.
// B. Exercise approvals, tool results, fresh workspaces and verification.
// C. Return a failing exit status if any check fails.

using MafClaw.Sample21.Tests;

return await Sample21Tests.RunAsync();
