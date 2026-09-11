# Session 03 troubleshooting

## Build fails because .NET 10 is unavailable

```powershell
dotnet --list-sdks
```

Install the .NET 10 SDK and run the build again.

## Shell sample rejects a command

This is expected when the command is not in the allowlist. The sample is
designed to fail closed; do not broaden the allowlist during a live demo.

## Background work says queued

That is the intended result. A ticket records accepted work; it is not a
completed research result.

## Live samples report a missing Foundry endpoint

Samples `11-skills-agent`, `21-confined-shell-agent`, `31-codeact-agent`, and
`41-background-agents` are live Agent Framework/Harness examples. From the
repository root, run:

```powershell
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
```

Samples `10`, `20`, `30`, and `40`, plus the complete advisor, remain offline
and do not need credentials.

## CodeAct sample (31) fails to start the sandbox

Sample 31 uses a Hyperlight micro-VM (`HyperlightCodeActProvider`), which
needs hardware virtualization. If it fails to launch the sandbox:

- Confirm your machine/VM has nested virtualization enabled (check with
  `systeminfo` on Windows and look for "A hypervisor has been detected").
- The sandbox is a minimal WASI-based Python runtime with a limited standard
  library (no `csv`, `pandas`, or filesystem access to the host) - this is by
  design. The agent reads data through the normal `file_access` tools and
  only uses the sandbox for computation.

## Shell sample (21) command needs approval every time

This is intentional: every `run_shell` call requires approval, and the
executor is confined to `working/confirmations` so it can never leave that
folder. Denying a command is a safe way to show the boundary during a demo.

## Privacy

Use only the included mock portfolio and watchlist. Never paste real holdings,
account identifiers, credentials, or confidential data into a sample.
