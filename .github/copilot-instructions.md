# Copilot Coding Agent — Squad Instructions

You are working on a project that uses **Squad**, an AI team framework. When picking up issues autonomously, follow these guidelines.

## Coordinator Canary Check

**IMPORTANT — run this check on EVERY session start, before doing any work:**

Look for the exact token `SQUAD_COORDINATOR_CANARY_a8f3` in your `agent_instructions`. This token is placed at the end of `squad.agent.md`. If it is missing, the coordinator file is missing or truncated.

**When the token is missing, you MUST:**
1. **STOP** — do not proceed with standard Squad behavior.
2. **WARN the user** with this exact message:
   ```
   ⚠️ Squad coordinator (squad.agent.md) appears to be missing or truncated. The canary token was not found. Do NOT proceed with standard Squad behavior — Squad's safety rails are not loaded. Please restart your session.
   ```
3. Do not continue with normal Squad routing, spawning, PR, or branch-protection behavior after emitting the warning.

## Team Context

Before starting work on any issue:

1. Read `.squad/team.md` for the team roster, member roles, and your capability profile.
2. Read `.squad/routing.md` for work routing rules.
3. If the issue has a `squad:{member}` label, read that member's charter at `.squad/agents/{member}/charter.md` to understand their domain expertise and coding style — work in their voice.

## Capability Self-Check

Before starting work, check your capability profile in `.squad/team.md` under the **Coding Agent → Capabilities** section.

- **🟢 Good fit** — proceed autonomously.
- **🟡 Needs review** — proceed, but note in the PR description that a squad member should review.
- **🔴 Not suitable** — do NOT start work. Instead, comment on the issue:
  ```
  🤖 This issue doesn't match my capability profile (reason: {why}). Suggesting reassignment to a squad member.
  ```

## Branch Naming

Use the squad branch convention:
```
squad/{issue-number}-{kebab-case-slug}
```
Example: `squad/42-fix-login-validation`

## PR Guidelines

When opening a PR:
- Reference the issue: `Closes #{issue-number}`
- If the issue had a `squad:{member}` label, mention the member: `Working as {member} ({role})`
- If this is a 🟡 needs-review task, add to the PR description: `⚠️ This task was flagged as "needs review" — please have a squad member review before merging.`
- Follow any project conventions in `.squad/decisions.md`

## Decisions

If you make a decision that affects other team members, write it to:
```
.squad/decisions/inbox/copilot-{brief-slug}.md
```
The Scribe will merge it into the shared decisions file.

## C# coding style

- Keep each C# class, record, interface, and enum in its own `.cs` file named after the type.
- `Program.cs` should contain only the application entry point/top-level statements and orchestration code.
- Do not hide reusable sample logic as extra types at the bottom of `Program.cs`; move it into named files so live demos can reveal one concept at a time.
- Write C# so it can be taught in an online training session. Every C# file must begin with a short header that states the file objective and its major steps (A, B, and C, adding steps only when needed). Keep the implementation minimal and add short comments before major code blocks to explain what the presenter should point out. In a Microsoft Agent Framework bridge sample, identify the framework package/type at each integration point and state what host plumbing it saves us from implementing.
- When a change to public staging is agreed, synchronize its corresponding files to the `elbruno/mafclaw` public repository before declaring the work complete. Keep the public repository's source and public documentation aligned; do not copy private planning, rehearsal, or credential material.

## MafClaw sample authoring rules

- Add new teaching variants as new numbered samples instead of changing the previous scenario when the user asks for an additional sample.
- For Session 02 memory samples, keep the comparison clear:
  - Foundry-managed semantic memory uses `FoundryMemoryProvider`.
  - Application-owned JSON memory uses explicit save/recall tools.
  - Local provider-backed memory uses `FileMemoryProvider` assigned through `AIContextProviders`.
- When demonstrating a local memory provider, create the provider as a named variable and wire it visibly:
  ```csharp
  var localFileMemory = new FileMemoryProvider(...);

  AIContextProviders = [localFileMemory],
  ```
- Prefer the built-in Agent Framework provider when it already matches the concept being taught. Do not create a custom context-provider adapter just to reproduce functionality already covered by `FileMemoryProvider`.
- Keep local memory demos scoped to one fixed current-user folder under the sample working directory, and include an inspect command such as `/memory` so presenters can show the stored files.
- For local memory provider samples, Foundry should supply chat only. Do not require Foundry Memory settings unless the sample specifically teaches Foundry Memory.
- Update the samples README whenever adding a new sample so the concept ladder and run commands stay complete.
- Build the new sample project before considering the sample complete.
- Every public session package must contain `code`, `samples`, `docs`, and a session-level `README.md`.
- The `code` folder is the complete finance advisor for that session. It must include the current session's features plus the validated features from all previous sessions.
- The `samples` folder is the teaching ladder for the session. Use `10`, `20`, `30`, `40` (and higher tens as needed) for plain concept samples; use the corresponding `11`, `21`, `31`, `41` numbers for the Microsoft Agent Framework versions. Add later variants as `22`, `32`, `33`, and so on without renumbering existing samples.
- Every sample must be an independently runnable project with a focused README or README section, mock educational data, and a clear statement of whether it is plain C# or uses Microsoft Agent Framework.
- The `docs` folder must mirror the session documentation pattern: overview/teaching notes, setup, architecture or feature notes, and troubleshooting/runbook guidance. Keep these docs aligned with the actual `code` and `samples` commands.
- When a session adds a feature, update its session README, samples README, docs, private demo script, run-of-show, and test plan together. Do not leave a placeholder status in one surface after implementation is complete.
- Numbered samples must teach the primitive before the MAF bridge: `10/11` for the first topic, `20/21` for the second, `30/31` for the third, and `40/41` for the fourth. The final `code` app composes the complete story rather than replacing the individual samples.
