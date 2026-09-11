# Session 03 architecture

The session keeps the same mock portfolio story as Session 02 and adds a
host-owned orchestration layer.

The complete `code` app carries forward the Session 02 boundaries first:
approved working-folder access, human approval for sensitive side effects, and
deliberate user-scoped memory. Session 03 then composes the new skill, shell,
CodeAct, and background-agent surfaces around them.

```text
skill catalog
      |
      v
portfolio check -> confined shell check -> background ticket
      |                 |                       |
      +-----------------+-----------------------+
                        v
                structured summary
```

## Boundaries

- **Skills:** descriptions are data; the host decides which implementation is
  available.
- **Shell:** the host owns the allowlist, working directory, timeout,
  cancellation, and output cap.
- **CodeAct:** calculations are explicit code with inspectable inputs and
  outputs rather than unsupported model arithmetic.
- **Background agents:** the queue returns a ticket and status. `queued` is not
  evidence that the work completed.

The complete app is intentionally host-driven and offline. A future live MAF
bridge can replace the fixed orchestration without widening these boundaries.
