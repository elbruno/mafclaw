# Session 2 isolated samples

These tiny samples illustrate the three core ideas behind the session.

## 1. Safe file access

`01-safe-file-access` demonstrates a working folder with allowed reads.

## 2. Approval gate

`02-approval-gate` models the human-in-the-loop approval flow before side effects.

## 3. Memory

`03-memory-store` shows durable state stored to disk and restored across a restart.

## Run them

From the `session-02` folder:

```powershell
dotnet run --project .\samples\01-safe-file-access\MafClaw.Sample01.csproj
dotnet run --project .\samples\02-approval-gate\MafClaw.Sample02.csproj
dotnet run --project .\samples\03-memory-store\MafClaw.Sample03.csproj
```

From the repository root:

```powershell
dotnet run --project .\session-02\samples\01-safe-file-access\MafClaw.Sample01.csproj
dotnet run --project .\session-02\samples\02-approval-gate\MafClaw.Sample02.csproj
dotnet run --project .\session-02\samples\03-memory-store\MafClaw.Sample03.csproj
```
