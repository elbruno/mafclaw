# Sample 31 - Automatic CodeAct execution in Hyperlight

This .NET 10 Microsoft Agent Framework sample reads the included mock
`holdings.csv` through scoped host file tools, then uses
`HyperlightCodeActProvider` to execute generated Python in a micro-VM.

**Current policy: `CodeActApprovalMode.NeverRequire`.** Generated sandbox code
runs automatically, without a human approval prompt. The startup banner states
that policy. Sandbox isolation is not human review or a general production
security guarantee. Use only the included mock educational data; this is not
financial advice.

From the public repository's `session-03` directory:

```powershell
dotnet run --project .\samples\31-codeact-agent\MafClaw.Sample31.csproj
```

Configure the existing Session 3 Foundry settings and Azure CLI identity first.
Hyperlight needs hardware virtualization. On Windows, use `dotnet run` or the
generated `.exe` apphost, not `dotnet MafClaw.Sample31.dll`.

Ask: `What is the total portfolio value, and what percent is in Technology?`
The seeded four-row fixture totals **$27,124.95**, with **66.01%** in Technology.
The console prints actual `TOOL CALL` and `TOOL RESULT` messages separately
from the assistant's answer. `/exit` closes the conversation.

The `FileSystemAgentFileStore` root remains the executable's `working`
directory, and the existing read-only host-tool auto-approval rule remains
in place. Any other approval requested by the framework is still presented
to the console user. Unrelated mode-switching, file-memory, todo, skills,
and web-search providers are disabled: this avoids a plan-first pause or
automatic planning-note writes and keeps the lesson on file access plus CodeAct.

To demonstrate approval before each generated-code execution instead, change
the visible assignment in `Program.cs` to `CodeActApprovalMode.AlwaysRequire`
and rebuild. The frozen Session 3 slides describe that earlier policy; their
approval narration is not the current default. See the current setup guide
and demo script rather than inferring the policy from an older slide.
