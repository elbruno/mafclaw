// Objective: enter Sample 45 without exposing raw service errors.
// A. Pass CLI arguments. B. Let the application own its lifecycle. C. Return a safe exit code.
using MafClaw.OrchestrationSupport;
using MafClaw.Sample45;

return await SafeConsole.RunAsync(() => SampleApplication.RunAsync(args));
