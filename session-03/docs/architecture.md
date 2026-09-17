# Session 03 architecture

The session keeps the same mock portfolio story as Session 02 and adds a
host-owned orchestration layer.

The complete `code` app carries forward the Session 02 boundaries first:
approved working-folder access, human approval for sensitive side effects, and
deliberate user-scoped memory. Session 03 then composes the new skill, shell,
CodeAct, and background-agent surfaces around them.

```text
file-based SKILL.md packages
      |
      v
advertise -> select -> load instructions/resources -> bounded host action
      |
      +-> confined shell check -> background ticket
      |                 |                       |
      +-----------------+-----------------------+
                        v
                structured summary
```

## Boundaries

- **Skills:** `SKILL.md` packages contain front matter, focused instructions,
  and optional references/scripts. Sample 10 shows their plain-C# discovery and
  host-owned execution; Sample 11 uses `AgentSkillsProviderBuilder` to expose
  the same packages to a live Harness agent through progressive disclosure.
- **Shell:** Sample 20 separates a CLI request from a host-owned allowlist.
  `CommandSpec` matches the executable and all arguments; the same matched
  object supplies `CommandRunner`'s process settings. Denial returns before
  launch. The runner captures both streams and terminates its owned process
  on timeout. Its working directory is an initial directory, not a sandbox;
  unlike the advisor's shell settings, this fixed-command sample has no
  output-size cap. Sample 21 separately exposes an explicit PowerShell
  `LocalShellExecutor` as an approval-gated `run_shell` tool. Its
  `ShellEnvironmentProvider` supplies real environment facts, and each run
  gets a fresh `working/confirmations/run-...` workspace. The console reports
  actual tool results, while host code verifies filenames and full content
  hashes. Do not present the two samples as identical policies or as a
  general OS-isolation guarantee.
- **CodeAct:** calculations are explicit code with inspectable inputs and
  outputs rather than unsupported model arithmetic. Sample 30 shows the
  boundary in plain C#; Sample 31 lets a live Harness agent read
  `holdings.csv` through the normal `file_access` tools, then write and run
  Python in a `HyperlightCodeActProvider` micro-VM sandbox (approval required
  on every execution) to compute the answer and show its work.
- **Background agents:** Sample 40 models the queue-and-status boundary in
  plain C#; Sample 41 hands a live Harness agent a lean `TickerResearchAgent`
  (a plain chat-client agent scoped to `HostedWebSearchTool`) through
  `HarnessAgentOptions.BackgroundAgents`, so it can fan research out per
  ticker, run those requests concurrently, and aggregate the results.

The plain-C# samples (`10`, `20`, `30`, `40`) stay host-driven and offline so
the boundary is visible without any live dependency. Their MAF-bridge
counterparts (`11`, `21`, `31`, `41`) are live Harness agents against a real
Azure AI Foundry project - see `setup.md` for configuring credentials.

### Sample 20 execution path

```text
CLI executable + arguments
          |
          v
independent host allowlist (exact match)
          |
          +-- no match --> DENIED, exit 2, no child process
          |
          +-- match ----> approved CommandSpec
                              |
                              v
                    ProcessStartInfo / Process.Start
                              |
                              +-- completes --> output + child exit code
                              |
                              +-- times out --> terminate owned process,
                                                wait for exit, CLI exit 124
```

`Process.Start` does not receive the allowlist and does not make an approval
decision. Validation belongs before the launcher. The command-line request
does not become a shell command string; only a matching host specification
can reach the runner.

## Reading the MAF bridge comments

### Sample 21: separate intention, permission and evidence

- `DemoWorkspace` seeds exactly four mock files in a new directory and keeps
  a baseline in application memory. It never deletes or resets prior runs.
- `LocalShellExecutor` uses `pwsh`, a 15-second timeout and a 4,096-byte
  per-stream output cap. Its denylist is only a prefilter.
- `ShellEnvironmentProvider` probes that same executor and contributes
  authoritative shell facts through `AIContextProviders`; the host does not
  invent a second environment-detection layer.
- `AsAIFunction(..., requireApproval: true)` keeps every model-generated
  shell call approval-gated. Standing auto-approval rules and unrelated
  default capabilities are disabled for this focused lesson.
- `ToolTranscript` displays complete proposed commands, approval decisions
  and real `FunctionResultContent` separately from assistant narration.
- `DemoVerifier` checks the actual names, lengths and SHA-256 hashes. Missing,
  duplicate, altered, unexpected or non-regular entries prevent a verified
  completion. Verification itself never renames files.

`ConfineWorkingDirectory` re-anchors each persistent-shell command to its
configured initial directory. It does not restrict every filesystem or OS
operation inside the script. Likewise, post-execution hash checks establish
the fixture outcome, not the absence of side effects elsewhere.

Teach each numbered pair as a comparison, not as a framework magic trick. The
plain sample first makes the capability and boundary visible; the MAF version
then labels the specific type that provides the reusable agent integration:

- **11:** `AgentSkillsProviderBuilder` handles skill discovery and progressive
  disclosure, and `AsHarnessAgent` supplies the agent/context routing loop.
- **21:** `LocalShellExecutor` supplies execution, `ShellEnvironmentProvider`
  supplies actual shell context, and `AsAIFunction`/`AsHarnessAgent` supply
  tool adaptation, invocation and approval. The host owns fixtures and verification.
- **31:** `HyperlightCodeActProvider` bridges an approval-gated sandbox into
  the agent, and `HarnessAgentOptions` composes it with file access and approval.
- **41:** `AsAIAgent` creates the focused worker and
  `HarnessAgentOptions.BackgroundAgents` provides background delegation and
  result collection.

The comments state what each type saves the application from implementing.
They do not imply that the framework makes safety decisions automatically: the
host still chooses the folders, policies, instructions, and approval settings.

## Why the comments are structured

The A/B/C headers mirror the teaching sequence: establish inputs, perform one
bounded action, and return an inspectable result. This keeps each primitive
visible in the source while the inline comments call out the trust boundary
being demonstrated.

## Main-agent orchestration variants

[Samples 42-47](orchestration.md) separate three concerns that should not be
collapsed into a single background-task label:

- **Delegation:** 42/43 use a named MAF `BackgroundAgentsProvider` to expose
  multiple specialists and collect results in separate sessions. The host
  releases the provider session when done.
- **Workflow policy:** 44/47 invoke MAF workers inside host-enforced phase
  and approval rules. A model cannot grant itself a further revision or a write.
- **Lifecycle:** 45/46 wrap live worker-agent runs in explicit host-owned
  job/cancellation/deadline policy. They do not invent per-task cancellation
  on the built-in provider or claim durable execution.

Actual tool events and host outcomes are printed independently from agent
narration. Fixture mode substitutes scripted inference/local workers and
labels that substitution; it never masks a failed live request.
