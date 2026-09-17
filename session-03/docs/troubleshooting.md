# Session 03 troubleshooting

## Build fails because .NET 10 is unavailable

```powershell
dotnet --list-sdks
```

Install the .NET 10 SDK and run the build again.

## Shell sample rejects a command

For Sample 20, this is expected unless the request is exactly the two tokens
`dotnet` and `--version`. Try `dotnet run -- dotnet --version` from its
directory. `dotnet run -- dotnet --info` intentionally returns exit code `2`
and `Process started: no.` The requested child process did not start.

Keep the `--` separator and do not quote the entire command as one token.
Extra arguments and shell operators are rejected, not parsed. Do not broaden
the allowlist merely to make an unexpected request pass during the demo.

## Sample 20 times out or reports an execution error

On a five-second timeout, Sample 20 terminates its owned process tree, waits
for the launched process to exit, and returns CLI exit code `124`. A timeout
is not reported as a successful command result.

For `Execution failed`, check that the .NET SDK is available on the trusted
machine's executable path. The launcher reports the error and returns `1`;
it does not fall back to a different executable. The initial working
directory is not a sandbox and does not grant additional OS permissions.

Run `dotnet run --project .\tests\Sample20.Tests\MafClaw.Sample20.Tests.csproj`
from the Session 03 directory for the offline regression checks.

## Background work says queued

That is the intended result. A ticket records accepted work; it is not a
completed research result.

## Live samples report a missing Foundry endpoint

Samples `11-skills-agent`, `21-confined-shell-agent`, `31-codeact-agent`, and
`41-background-agents` are live Agent Framework/Harness examples. From the
repository root, run:

```powershell
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
```

Samples `10`, `20`, `30`, and `40`, plus the complete advisor, remain offline
and do not need credentials.

## CodeAct sample (31) fails to start the sandbox

Sample 31 uses a Hyperlight micro-VM (`HyperlightCodeActProvider`), which
needs hardware virtualization. If it fails to launch the sandbox:

- Confirm your machine/VM has nested virtualization enabled (check with
  `systeminfo` on Windows and look for "A hypervisor has been detected").
- The sandbox is a minimal WASI-based Python runtime with a limited standard
  library (no `csv`, `pandas`, or filesystem access to the host) - this is by
  design. The agent reads data through the normal `file_access` tools and
  only uses the sandbox for computation.

## Shell sample (21) command needs approval every time

This is intentional, including inspection calls. The model's description of
a command as "read-only" does not approve it. Review the full PowerShell
script and respond `y`/`yes` or `n`; Enter denies it. Denying the rename should
leave `HOST CHECK: NOT COMPLETE`, not a false execution or completion report.

## Sample 21 mixes shell syntax or cannot start PowerShell

The updated sample explicitly launches `pwsh` and wires the built-in
`ShellEnvironmentProvider` into `AIContextProviders`. Check the startup shell
version and ensure PowerShell 7 is installed and on PATH. There is no silent
fallback to another shell. If a model still proposes Unix syntax, reject the
command and direct it to the reported PowerShell environment.

## Rebuilding Sample 21 reports a locked executable on Windows

Exit the already-running sample with `/exit`, then run `dotnet run` again.
The running app can lock its executable; this is not a NuGet or source-code
failure. Do not delete its workspace or forcibly terminate unrelated processes.
An already-running instance keeps its old code until it is restarted.

## Sample 21 files differ from a previous run

Each run creates a new directory under `working\confirmations`. Use the
printed path. Earlier run folders and old flat-layout files are retained,
not mixed into the new fixture or automatically deleted.

## The assistant says done but Sample 21 does not say HOST VERIFIED

The assistant's summary is not verification. The host requires all four
expected names and the original full SHA-256 hashes, with no missing,
duplicate, changed, unexpected or linked entries. Inspect the listed issues
or use `/verify`. Do not clear a failing check by weakening it or deleting an
unknown directory. A fresh application run creates a separate clean fixture.

`TOOL RESULT` is the real executor result, including nonzero exit codes,
errors, timeouts and truncation when reported. It does not imply that the
entire rename task succeeded. The fixture contents have no trade dates;
workspace timestamps and file metadata must not be presented as trade dates.

## Privacy

Use only the included mock portfolio and watchlist. Never paste real holdings,
account identifiers, credentials, or confidential data into a sample.

## When comments and code drift

If a presenter cannot follow a sample from its objective and A/B/C header, treat
that as a documentation defect. Keep the code minimal, restore the header and
major-block comments, and rerun the sample build before rehearsal.
