# Configuration and repository verification

Run these commands from the public repository root with PowerShell 7 and the
.NET 10 SDK. The planning repository has forwarding scripts under `tools` that
use `public-staging` automatically.

```powershell
.\tools\configure-user-secrets.ps1 -WhatIf
.\tools\configure-user-secrets.ps1
.\tools\configure-user-secrets.ps1 -Check
.\tools\verify-repository.ps1 -Mode Inventory
.\tools\verify-repository.ps1 -Mode Offline
.\tools\audit-packages.ps1 -IncludeAdvisories
```

With no `-Session` argument, setup configures **all sessions (1-4)** and all
their declared cloud-backed projects, including the settings stores used by
shared helpers. Each unique setting is collected once and reused wherever
needed. Supply the required endpoint/model; optional service settings can be
skipped with Enter, leaving existing values unchanged. Offline-only projects
need no secrets, and optional services are not provisioned or enabled automatically.

Use `-Session 4` (or another number) for a targeted run. `-Check` and `-WhatIf`
also cover all sessions by default. `-ProjectPath` requires an explicit numbered
session. Removal always requires an explicit selection: for example,
`-Session 4 -Clear` or `-Session All -Clear`.

`Inventory` discovers source projects independently of solution membership and
asks MSBuild for the effective target frameworks and package references,
including central package versions. It reports runnable projects without cases.
Every maintained project must target `net10.0`.

`Build` restores/builds all projects in Release. `Offline` additionally runs
all declared deterministic checks; it is the default. `Live` runs only live
cases after building; `All` runs both sets. These last two modes explicitly
permit real model/service calls and their associated costs. Configure the
required services first. Unselected cases are `NotRun`, never Pass.
Use `-CaseId` with exact IDs from the inventory/manifest report to select a
bounded subset of cases; unknown IDs fail rather than quietly selecting none.
Case selection does not skip the repository build or claim unselected coverage.

All non-inventory modes require each executable/test project to have a
verification case. A host exercised through an integration runner can be named
in that case's `coversProjects`. Libraries are always built. Tests use the
runner in the manifest: an executable test program uses `run`, not `dotnet test`.
Build failures block dependent cases instead of running stale binaries.

The package audit requires restored projects. It inventories resolved direct
and transitive packages, checks the latest applicable public NuGet versions,
and optionally checks vulnerability/deprecation metadata. Stable dependencies
stay on the stable channel; existing preview dependencies are compared with
the latest preview/stable candidate. Older direct dependencies fail the audit.
Transitive update candidates require review through their owning dependency;
the audit never adds unsupported forced pins. A failed lookup is not a clean bill
of health. The script does not automatically approve compatibility exceptions.

Reports go to `.local\verification` by default, or to an explicit `-ReportPath`.
Captured application stdout/stderr are deliberately absent from saved reports:
live Azure errors can include infrastructure identifiers. Results include
project/case identifiers, versions, durations, exit codes and failed assertions.
Rerun a failing command in a private terminal for detailed diagnostics.
These reports are excluded from publication; do not copy raw live logs into docs.

## Verification manifest contract

Each session supplies `verification-manifest.json`, version 1:

```json
{
  "schemaVersion": 1,
  "cases": [
    {
      "id": "offline-example",
      "project": "samples\\10-example\\Example.csproj",
      "mode": "offline",
      "runner": "run",
      "args": [],
      "stdin": "",
      "timeoutSeconds": 180,
      "expectedExitCode": 0,
      "requiredOutput": ["PASS"],
      "forbiddenOutput": ["FAIL"],
      "coversProjects": []
    }
  ]
}
```

Project paths are relative to the session containing the manifest. Absolute
paths and paths outside that session are rejected. Arguments are passed as
individual process arguments, not interpolated into a shell command.
Input is redirected and closed, including when no input was provided.
Timeouts terminate the owned process tree. Output assertions are literal and
case-sensitive, and can check intentional nonzero exit codes.
`minimumOutputLength` optionally requires nonempty/substantive stdout when an
exact model phrase is inappropriate; stderr cannot satisfy that requirement.
An optional `environment` object supplies **synthetic values only** to the
child process, without changing the parent environment. A construction-only
case can use an `example.invalid` endpoint and immediately send `/exit` to
exercise real provider composition without inference or credentials.

Use only synthetic inputs. Do not place endpoints, credentials, real financial
data, or account identifiers in a manifest. Live checks must not read unrelated
user files or mutate real financial state.
