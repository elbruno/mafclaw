# Session persistence

Samples 60/61 teach conversation/session persistence: saving conversation
state and resuming it later, without replaying every prior turn by hand.
This is distinct from Session 2's **memory** samples
(`FoundryMemoryProvider`, `FileMemoryProvider`, JSON tool memory), which are
about *long-term recall across otherwise-unrelated sessions* (facts a user
explicitly asked the agent to remember). Session persistence here is about
*resuming the same conversation/thread itself* — the difference between "the
agent remembers my name from last week" (memory) and "this is literally the
same conversation, continued" (session persistence).

## 60: what "session state" is, in plain C#

`MafClaw.Sample60` has no model and no agent framework. It keeps an explicit,
serializable list of turns (`Turn.cs`), writes it to a JSON file after every
turn, and reloads it with `--resume`:

```powershell
dotnet run --project .\samples\60-session-persistence\MafClaw.Sample60.csproj
dotnet run --project .\samples\60-session-persistence\MafClaw.Sample60.csproj -- --resume <path-from-previous-run>
dotnet run --project .\samples\60-session-persistence\MafClaw.Sample60.csproj -- --self-test
```

`--self-test` runs two turns, reloads the transcript from disk (simulating a
process restart), runs one more turn, and verifies the reloaded transcript
kept counting from where it left off. Fully offline and deterministic.

## 61: an actual MAF `AgentSession`

`MafClaw.Sample61` uses the Microsoft Agent Framework's own session
persistence surface instead of a hand-rolled message list:

- `agent.CreateSessionAsync()` creates an `AgentSession`.
- `agent.SerializeSessionAsync(session)` returns a `JsonElement` you can
  write anywhere (file, database, cache).
- `agent.DeserializeSessionAsync(element)` restores an `AgentSession` that,
  when reused, continues the same conversation — the harness/agent resends
  the prior history to the model, so you never manually replay turns.

```powershell
dotnet run --project .\samples\61-session-persistence-agent\MafClaw.Sample61.csproj -- --fixture --self-test
dotnet run --project .\samples\61-session-persistence-agent\MafClaw.Sample61.csproj -- --live --save <path>
dotnet run --project .\samples\61-session-persistence-agent\MafClaw.Sample61.csproj -- --live --resume <path>
```

The fixture's scripted replies are fixed text, so a passing run alone would
not prove real persistence. `RecordingChatClient` wraps the chat client and
records the exact messages sent to it; `--self-test` asserts that after
`DeserializeSessionAsync`, the next model call actually included the earlier
turns ("My name is Ada.", "I prefer email over calls.") — proving the
restored session, not the scripted answer, carries the history forward.

## Choosing between them

Use plain transcript persistence (60) when you own the full message loop and
want simple, inspectable JSON. Use `AgentSession` persistence (61) once you
are building on a MAF agent/harness: it also carries harness-owned state
(todos, tool-approval state, context-provider state), not just chat history,
and its serialized shape is provider-defined, so treat it as opaque and
store it like any other application secret-adjacent data — see
[Session | Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/concepts/agents/conversations/session)
for the service-session-ID scoping caveats in hosted/multi-user apps.
