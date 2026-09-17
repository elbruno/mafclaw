# Orchestration sample support

Shared plumbing for Samples 42-47, not a seventh scenario or a replacement
for their orchestration code. Each numbered project can be run directly
with `dotnet run --project ...`; its project reference builds this library.

- `FoundryConnection` uses the existing Session 3 settings and converts the
  Foundry Responses client to `IChatClient`.
- `TracingChatClient` observes actual function requests/results without
  logging system instructions or ordinary conversation bodies.
- `Transcript` serializes concurrent console events and bounds retained history.
- `FixtureChatClient` replaces inference with an explicitly supplied script.
  It is not a model, not live research, and not a fallback after a failed
  Foundry request. The MAF tool/agent pipeline still handles the script.
- `SafeConsole` reports expected failures without raw cloud/account details.

Role definitions, worker tools, routing, lifecycle state, deadlines, review
rules, approvals, fixtures, and verification belong in the numbered samples.
Keep the `OrchestrationSupport` folder when copying these samples elsewhere.

The transcript is for mock educational inputs only. Tool arguments and
results may contain sensitive content in a real application; this is not
a production redaction or audit-log system.
