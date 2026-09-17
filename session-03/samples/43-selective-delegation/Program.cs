// Objective: enter the educational selective-delegation console.
// A. Pass arguments to the sample host.
// B. Sanitize expected infrastructure errors with shared SafeConsole.
// C. Return an explicit process exit code.

using MafClaw.OrchestrationSupport;
using MafClaw.Sample43;

return await SafeConsole.RunAsync(() => SampleApp.RunAsync(args, Console.In, Console.Out));
