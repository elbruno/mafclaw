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

## Skills bridge reports a missing Foundry endpoint

Sample `11-skills-agent` is intentionally a live Agent Framework/Harness
example. From the repository root, run:

```powershell
az login --output none
.\tools\configure-user-secrets.ps1 -Session 3
```

Samples `10`, `20`, `21`, `30`, `31`, `40`, and `41`, plus the complete
advisor, remain offline and do not need credentials.

## Privacy

Use only the included mock portfolio and watchlist. Never paste real holdings,
account identifiers, credentials, or confidential data into a sample.
