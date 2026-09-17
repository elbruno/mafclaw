// Objective: enter the educational specialist-team console.
// A. Pass command-line arguments to the sample host.
// B. Let shared SafeConsole sanitize expected infrastructure failures.
// C. Return the host's explicit success or failure exit code.

using MafClaw.OrchestrationSupport;
using MafClaw.Sample42;

return await SafeConsole.RunAsync(() => SampleApp.RunAsync(args, Console.In, Console.Out));
