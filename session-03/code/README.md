# Session 03 code sample

Offline .NET sample for Episode 3. The complete advisor carries forward the
Session 2 file-access, approval, and memory boundaries before adding Session 3
orchestration.

Target framework: .NET 10.

## Scope

- local skill catalog for readable demo skills
- mock watchlist inspection
- Session 2 continuity: approved working folder, approval boundary, and
  user-scoped memory boundary
- allowlisted shell execution with a fixed working directory, timeout, and output cap
- observable in-memory background-task queue
- compact orchestration summary for a CodeAct-style step

## Run

```powershell
dotnet run --project .\MafClaw.Session03.csproj
```

The sample is self-contained and makes no model or network calls. It executes
only the configured `dotnet --version` command, using the application output
directory as its working directory. The background task is local and
short-lived; its returned `queued` status is intentionally not changed in the
printed result.

The implementation is intentionally host-driven: the catalog describes skills,
while the application owns tool validation, process limits, and workflow status.

## How to teach the code

The source is intentionally small and host-driven. Each C# file begins with
an objective plus A/B/C steps, and major blocks have short comments that map
the code to the spoken explanation: load inputs, apply one boundary, and
return an inspectable result.
