// Objective: run dependency-free Sample 20 regression checks.
// Steps:
// A. Select the synthetic child-process fixture only when explicitly requested.
// B. Otherwise run the checks and return their exit status.
// C. Report failure through the process exit code.

using MafClaw.Sample20.Tests;

if (args is ["--fixture", var mode])
{
    return await ProcessFixture.RunAsync(mode);
}

return await Sample20Tests.RunAsync();
