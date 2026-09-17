// Objective: run the ordered research/writer/reviewer teaching sample.
// A. Parse the console contract without loading credentials in fixture mode.
// B. Delegate bounded orchestration to the sample host.
// C. Sanitize expected failures at the console boundary.

using MafClaw.OrchestrationSupport;
using MafClaw.Sample44;

return await SafeConsole.RunAsync(() => SampleConsole.RunAsync(args, Console.In, Console.Out));
