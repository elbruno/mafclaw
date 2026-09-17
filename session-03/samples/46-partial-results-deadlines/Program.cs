// Objective: enter Sample 46 through sanitized console error handling.
// A. Forward CLI options. B. Delegate lifecycle orchestration. C. Return a safe process exit code.
using MafClaw.OrchestrationSupport;
using MafClaw.Sample46;

return await SafeConsole.RunAsync(() => SampleApplication.RunAsync(args));
