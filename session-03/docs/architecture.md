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
- **Shell:** the host owns the allowlist, working directory, timeout,
  cancellation, and output cap. Sample 20 walks the boundary in plain C#;
  Sample 21 confines a live `LocalShellExecutor` to a seeded
  `working/confirmations` folder and exposes it as an approval-gated
  `run_shell` tool, so a Harness agent can reorganize files but never escape
  the confined root.
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

## Reading the MAF bridge comments

Teach each numbered pair as a comparison, not as a framework magic trick. The
plain sample first makes the capability and boundary visible; the MAF version
then labels the specific type that provides the reusable agent integration:

- **11:** `AgentSkillsProviderBuilder` handles skill discovery and progressive
  disclosure, and `AsHarnessAgent` supplies the agent/context routing loop.
- **21:** `LocalShellExecutor` and `AsAIFunction` provide the confined shell
  tool contract, while `AsHarnessAgent` handles its invocation and approval flow.
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
