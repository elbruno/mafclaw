// Objective: demonstrate that multi-agent analysis does not confer write permission.
// A. Parse live/fixture console options.
// B. Run the bounded report-and-human-approval host.
// C. Sanitize expected failures without exposing configuration or raw errors.

using MafClaw.OrchestrationSupport;
using MafClaw.Sample47;

return await SafeConsole.RunAsync(() => SampleConsole.RunAsync(args, Console.In, Console.Out));
