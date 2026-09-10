# Session 2 samples

These samples use a concept-to-agent teaching ladder:

- `10`, `20`, and `30` are small plain C# demos that isolate the concept.
- `11`, `21`, `31`, and `32` implement those ideas with Microsoft Agent Framework, Harness, or Foundry APIs.
- `..\code\` is the complete finance advisor that combines all three concepts.

## Sample authoring rule

Every live-demo sample should be readable while the presenter is sharing the screen:

- start the main code file with a short A/B/C/D flow summary
- add simple inline comments before major code blocks, not on every line
- explain boundaries, setup, tool registration, and persistence points where they appear in code
- keep comments short enough that they help narration without hiding the code

## 10 and 11 — safe file access

- `10-safe-file-access` demonstrates a working folder with allowed reads.
- `11-safe-file-access-agent` uses the Harness `FileAccessStore` with the built-in `file_access_*` tools rooted at the approved working folder.
- For the live demo, ask `What is in my portfolio?` first, then copy the `Denied prompt` printed by sample 11 to show the agent refusing a harmless path outside the approved root.

## 20 and 21 — approval gates

- `20-approval-gates` models the human-in-the-loop approval flow before side effects.
- `21-approval-gates-agent` wraps the simulated trade tool with `ApprovalRequiredAIFunction` so Harness owns the approval boundary.
- For the live demo, run `Buy 10 shares of MSFT.` twice: answer `y` once for the approved path, then answer `n` for the denied path.

## 30, 31, and 32 — memory store

- `30-memory-store` shows durable state stored to disk and restored across a restart.
- `31-memory-store-agent` saves `Remember...` prompts as Foundry `UserProfile` memories, prints the saved scope/count, and uses `FoundryMemoryProvider` to recall durable user facts when memory is configured.
- `32-local-file-memory-agent` gives the agent explicit current-user save/recall tools backed by an inspectable local JSON file.
- For the reliable live demo, run sample 32: save the profile, enter `/memory`, restart, recall it, then ask `What do you remember about other users?`.
- Use sample 31 to compare the local application-owned approach with managed Foundry Memory.

## Run them

Configure secrets once before running the agentic samples:

```powershell
.\tools\configure-user-secrets.ps1 -Session 2 -WhatIf
.\tools\configure-user-secrets.ps1 -Session 2
```

From the `session-02` folder:

```powershell
dotnet run --project .\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet run --project .\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj

dotnet run --project .\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet run --project .\samples\21-approval-gates-agent\MafClaw.Sample21.csproj

dotnet run --project .\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet run --project .\samples\31-memory-store-agent\MafClaw.Sample31.csproj
dotnet run --project .\samples\32-local-file-memory-agent\MafClaw.Sample32.csproj
```

From the repository root:

```powershell
dotnet run --project .\session-02\samples\10-safe-file-access\MafClaw.Sample10.csproj
dotnet run --project .\session-02\samples\11-safe-file-access-agent\MafClaw.Sample11.csproj

dotnet run --project .\session-02\samples\20-approval-gates\MafClaw.Sample20.csproj
dotnet run --project .\session-02\samples\21-approval-gates-agent\MafClaw.Sample21.csproj

dotnet run --project .\session-02\samples\30-memory-store\MafClaw.Sample30.csproj
dotnet run --project .\session-02\samples\31-memory-store-agent\MafClaw.Sample31.csproj
dotnet run --project .\session-02\samples\32-local-file-memory-agent\MafClaw.Sample32.csproj
```
