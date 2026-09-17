# Sample 20 - A visible command allowlist

**Plain C#, .NET 10. No Microsoft Agent Framework, model call, credentials,
financial data, or human approval prompt.**

This sample separates three things: a caller's request, the host's policy,
and the process launcher. The policy permits exactly `dotnet --version`.
It is defined independently of the command-line arguments.

## Run the two teaching cases

From this sample's directory:

```powershell
# Allowed: the matching process starts and reports the installed SDK version.
dotnet run -- dotnet --version

# Denied: this is outside the policy, so no requested process starts.
dotnet run -- dotnet --info
$LASTEXITCODE # 2 is the expected policy-denial exit code.
```

`dotnet --info` is rejected because it is not on this sample's allowlist,
not because that command is inherently dangerous.

Running `dotnet run` without arguments still performs the allowed version
check, preserving the original quick-start and fallback command.

Pass the executable and arguments as separate tokens after `--`. The sample
does not parse a quoted command string, expand shell syntax, or interpret
operators such as `;` or `&&`. Matching is ordinal and case-sensitive; extra,
missing, or changed tokens are denied.

The denied case reports:

```text
Requested: dotnet --info
Policy: DENIED. Only dotnet --version is allowed.
Process started: no.
```

The .NET CLI and sample application are already running at that point.
"Process started: no" means the **requested child process** was not launched.

## Walk through the source

1. **`Program.cs`** defines the host-owned allowlist, reads the independent
   request, and returns immediately on denial. There is no approval prompt:
   this is a policy decision, not a human decision.
2. **`CommandSpec.cs`** compares both the executable and the complete argument
   list. The matching object is the one passed to the runner.
3. **`CommandRunner.cs`** constructs `ProcessStartInfo` from that object.
   `Process.Start` launches the approved operation; it does not inspect the
   allowlist or make a permission decision.
4. **`CommandResult.cs`** records the PID, actual child exit code, stdout,
   stderr, and whether the execution timed out.

Presenter line:

> The caller requests an operation. The host checks its policy. Only an
> allowed operation reaches the process launcher.

## What the boundary does and does not mean

- The working directory is `AppContext.BaseDirectory`, normally the build
  output folder. It is an initial directory, **not filesystem confinement**.
- `UseShellExecute = false` starts the executable directly. It does not
  provide a sandbox or remove the process's existing OS permissions.
- The executable name `dotnet` uses the machine's trusted .NET installation
  and normal executable resolution. This sample does not pin an executable
  by absolute path or hash.
- Both output streams are drained while the process runs, avoiding a full
  pipe blocking exit. This small fixed-command demo does not impose an
  output-size cap or accept arbitrary programs.
- A five-second timeout requests termination of the sample-owned process
  tree and waits for the launched process to exit before returning. Merely
  cancelling `WaitForExitAsync` would not terminate it.
- The CLI returns the child exit code on normal completion, `2` for denial,
  `124` for timeout, or `1` for a reported launch/execution error.

Sample 21 is the next lesson: a live MAF `LocalShellExecutor` tool with
configured directory/command policy and human approval. It is not the same
policy as this one-command allowlist.

## Offline regression checks

From the Session 03 directory:

```powershell
dotnet run --project .\tests\Sample20.Tests\MafClaw.Sample20.Tests.csproj
```

The dependency-free test executable exercises the default and explicit
allowed CLI paths, several rejected requests, exact argument matching,
stdout/stderr and exit-code propagation, large output, the initial working
directory, launch failures, invalid limits, and actual termination of a
synthetic long-running child and its descendant. Fixtures are test-only, not extra allowlisted
commands in Sample 20.
