# Sample 21 - Inspect, propose, approve, execute, verify

**Microsoft Agent Framework + Harness, .NET 10, PowerShell 7, live Foundry model.**
All four confirmations are synthetic educational data. No real trade records
or financial advice are involved.

Sample 20 checks a caller-supplied command against an exact allowlist.
Here the user supplies a goal and the model proposes the PowerShell commands.
The application retains the approval decision and independently checks the
result. It does not contain a hard-coded renaming implementation.

## Setup and run

Install PowerShell 7 so `pwsh` is on the executable path, including on Linux
or macOS. The sample explicitly selects PowerShell; it does not silently fall
back to bash, Windows PowerShell, or cmd.

Configure Foundry from the public repository root before presenting:

```powershell
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
dotnet run --project .\session-03\samples\21-confined-shell-agent\MafClaw.Sample21.csproj
```

Enter:

```text
Tidy up my trade confirmations.
```

The startup banner shows the configured shell, its observed version, and the
new workspace path. Each run creates a fresh
`working\confirmations\run-<timestamp>-<guid>` directory under the application
output directory. Previous directories and old flat-layout files are left
untouched. Use the printed path when opening the files, not a previous run's
folder. There is no reset command that deletes earlier data.

## The four-act demo

1. **Inspect:** `BEFORE` lists four inconsistent names with baseline hash
   fingerprints. The agent requests a compact PowerShell read of the names
   and contents. Review the exact command before approving it.
2. **Propose:** the agent describes its source-to-destination mapping, then
   requests a rename batch. It must check source files and target collisions
   before renaming, without editing contents or overwriting files.
3. **Approve and execute:** `PROPOSED COMMAND #N` shows the full script.
   `APPROVAL #N` records your decision. `TOOL RESULT #N` shows the real MAF
   `FunctionResultContent`, including the executor's `exit_code`, output,
   errors, and timeout/truncation flags when present. It is not model prose.
4. **Verify:** the host checks expected filenames, byte lengths and full
   SHA-256 hashes against its in-memory baseline. Only a complete match
   prints `HOST VERIFIED: 4/4`. Otherwise it prints `HOST CHECK: NOT COMPLETE`
   and specific pending or failed checks.

The intended path needs an inspection call and a rename call, but model
behavior can vary. Review every additional command rather than blindly
approving retries. The model is asked to stop after renaming; the application
does the final verification without another model or shell call.

All shell calls made by the model require approval, including reads.
Pressing Enter or `n` denies a request; EOF stops without submitting pending
commands. Whitespace around `y` or `yes` is accepted. To demonstrate denial,
decline the rename: the host should report that renaming is not complete.

Console commands:

- `/verify` runs the host check again without calling the model.
- `/exit` ends the conversation and disposes the shell.
- Mode switching is intentionally absent from this focused sample.

## Read the code in this order

| File | Lesson |
|---|---|
| `Program.cs` | `LocalShellExecutor` -> `ShellEnvironmentProvider` -> approval-gated `AsAIFunction` -> `AsHarnessAgent` |
| `DemoInstructions.cs` | A narrow renaming goal, correct shell syntax, no invented dates, and no model-authored verification claim |
| `AgentConsoleRunner.cs` | MAF-owned conversation and approval flow, with distinct assistant, tool-result and host-check output |
| `ToolTranscript.cs` | Full commands before approval; actual results correlated by call ID |
| `DemoWorkspace.cs` / `DemoFile.cs` | Fresh fixtures and immutable baseline records; never rename or delete earlier runs |
| `DemoVerifier.cs` | Observe the filesystem and compare full hashes; never perform the rename |

The built-in `ShellEnvironmentProvider` probes the actual executor and adds
its environment facts to `AIContextProviders`. These are known host startup
probes, not model-generated commands. This avoids a hand-written context
adapter and prevents the model from guessing between Unix and PowerShell.
The seed contents contain no trade dates; the workspace timestamp is a run
identifier, not a transaction date.

## Permissions and limits

- `AsAIFunction(..., requireApproval: true)` retains MAF's approval wrapper.
  `DisableToolAutoApproval = true` disables standing auto-approval rules,
  not the approval requirement.
- Unrelated default skill, memory, todo, web-search and mode providers are
  disabled so the lesson stays on the one shell tool.
- `ConfineWorkingDirectory = true` re-anchors persistent-shell commands to
  the configured initial directory. It is **not filesystem or OS isolation**.
- The denylist is a small prefilter for obvious off-task commands, not a
  security boundary or a PowerShell parser. Instructions do not enforce
  permissions either. Review the full script, including paths and effects.
- The executor has a 15-second per-command timeout and a 4,096-byte
  per-stream capture limit. Truncation is labeled by the SDK.
- The verifier checks only the current fixture directory, rejects unexpected
  entries and links, and never reads unknown file contents. It does not prove
  that a shell command had no effects elsewhere on the machine.

Only approve commands you understand, operating on the printed mock workspace.
Use stronger isolation for untrusted scripts; do not describe this demo as a
sandbox.

## Offline regression checks

From the Session 03 directory:

```powershell
dotnet run --project .\tests\Sample21.Tests\MafClaw.Sample21.Tests.csproj
```

These use real MAF and PowerShell with deterministic fake model responses:
no Foundry credentials or model charges. They cover fresh-run preservation,
expected names and hashes, same-length tampering, missing/duplicate/unexpected
entries, shell environment facts, actual failures/timeouts, approval before
execution, denial, EOF, and refusal to treat model claims as verification.
They are not a substitute for a live rehearsal against the configured model.
