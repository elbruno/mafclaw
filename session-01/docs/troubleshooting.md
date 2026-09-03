# Session 1 Troubleshooting

These samples have **no try/catch or error-handling wrapper**. Failures surface as raw .NET exceptions.

## ⚠️ Privacy warning — read this first

**Raw Azure exceptions can contain tenant IDs, account names, resource identifiers, and endpoint URLs.** If you are streaming, recording, or screen-sharing:

1. **Stop sharing before investigating errors.**
2. Copy the error message to a private window.
3. Never paste raw exception output into issues, chat, or screenshots.

This warning replaces the sanitized error handling (`SafeErrors`, `ErrorDispatch`, `AuthErrorFormatter`) that was removed from the code to keep it simple for teaching.

---

## Not logged in to Azure CLI

**Error:** `Azure.Identity.CredentialUnavailableException` — Azure CLI not logged in or token expired.

**Fix:**

```powershell
az login --output none
```

Then retry. Do not share the `az login` output — it may contain account and tenant details.

## Missing user-secrets

**Error:** `System.NullReferenceException` on `config["Foundry:ProjectEndpoint"]!` or the app crashes immediately.

**Fix:** From the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

Verify the keys are set (without printing values):

```powershell
dotnet user-secrets list --project .\session-01\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

## Model not deployed or unavailable

**Error:** `Azure.RequestFailedException` with a 404 or "model not found" message, or `ClientResultException`.

**Fix:**

- Confirm the model name in your user-secrets matches a deployed model in your Foundry project. The default is `gpt-5-mini`.
- Check the Foundry portal to verify the deployment exists and your identity has access.
- Do not share the error output — it may contain your project endpoint.

## Content filter or safety policy refusal

**Error:** `Azure.RequestFailedException` with a 400 and a content-filter message.

**Fix:** Respect the refusal. Do not repeatedly rephrase or try to bypass the policy. Choose a clearly benign educational prompt. If the request is still refused, stop.

## Wrong or invalid endpoint

**Error:** `Azure.RequestFailedException` with connection refused, DNS resolution failure, or 401/403.

**Fix:**

- Re-run the setup script: `.\tools\configure-user-secrets.ps1 -Session 1`
- The endpoint must be an absolute HTTPS URI pointing to your Foundry project.
- Check network, proxy, and firewall settings.

## Hosted search not working

Hosted web search is enabled by default through the Harness, but the configured model/service must support it. If you don't see search results:

- Try a clearly time-sensitive prompt: `Find recent public market context for NVDA and cite the sources.`
- The model may choose not to use search if it can answer from training data.
- Search can incur additional Azure charges.

## `/todos` says "No todos yet."

`TodoProvider` is configured by the Harness. A todo list appears only after the model creates items during a multi-turn conversation. Try a multi-step request:

```
Plan how to review MSFT and NVDA, including prices, recent context, and risks.
```

Then run `/todos` to see tracked items.

## Build or restore fails

Confirm .NET 10 is installed:

```powershell
dotnet --list-sdks
```

Restore and build a checkpoint:

```powershell
dotnet build .\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

If NuGet restore fails, check the `NuGet.Config` in `code/` or use:

```powershell
dotnet restore .\code\MafClaw.Session01.csproj --configfile .\code\NuGet.Config
```

## "OPENAI001" or "MAAI001" warnings

These are expected preview-package warnings. They are suppressed via `<NoWarn>` in each `.csproj` file. If you see them, you may be building from a modified project file.

[Back to Session 1](../README.md) | [Setup](./setup.md)
