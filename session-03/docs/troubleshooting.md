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

## MAF bridge sample does not call a model

That is intentional. The bridge samples demonstrate tool registration and
boundary ownership without requiring credentials or incurring cloud cost.
Model-backed `AsHarnessAgent` wiring belongs in the complete app after the
corresponding preview API is validated.

## Privacy

Use only the included mock portfolio and watchlist. Never paste real holdings,
account identifiers, credentials, or confidential data into a sample.
