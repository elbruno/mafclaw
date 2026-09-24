# Session persistence

From either `60-session-persistence` or `61-session-persistence-agent`, run:

```powershell
dotnet run
```

Each program saves two exchanges, restores state from JSON and continues the
conversation in one invocation. No mode switches or save/resume arguments
are needed. The printed file under `.local\sessions` stays available for the
presenter to inspect; each run gets a unique filename.

## 60: plain C# transcript

The host owns a `List<Turn>`, saves it using `JsonSerializer`, reloads a fresh
list and adds a third exchange. The assertion requires four messages before
restore and six after continuing. Replies are clearly deterministic, with no
model involved.

## 61: MAF `AgentSession`

The entry point directly builds the Harness and demonstrates:

1. `CreateSessionAsync` creates a conversation.
2. Two `RunAsync` calls establish a name and contact preference.
3. `SerializeSessionAsync` produces the framework-owned JSON state.
4. `DeserializeSessionAsync` restores a fresh session from disk.
5. A third `RunAsync` continues that restored conversation.

`RecordingChatClient` observes the exact messages sent on the last model
request. A PASS requires both earlier user facts in that outbound history;
plausible prose alone does not prove session restoration.

The cloud connection uses Chat Completions, leaving history with the Harness.
It does not depend on the provider's Responses conversation store. The previous
Responses path returned HTTP 500 on the configured deployment; the local
session demonstration needs neither that endpoint nor server-side storage.

`--fixture --self-test` is retained for automated offline checks. It scripts
inference but exercises real serialization/restoration and deletes only its
own temporary file. Normal `dotnet run` uses the configured live model.

## Session state is not semantic memory

This resumes the **same conversation**, unlike the Session 2 memory examples
that recall selected facts across different conversations. The serialized
MAF schema can include provider-owned state, so store it as opaque data rather
than depending on its internal shape. Use synthetic facts on stream; never
publish conversation files containing private data.
