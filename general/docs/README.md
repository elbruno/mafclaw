# Setup and prerequisites

This guide covers prerequisites, configuration, and troubleshooting for all MafClaw sessions.

## Target stack

- **.NET 10** SDK and runtime
- **C#** (language)
- **Azure Foundry** project and model
- **Azure CLI** (`az`) for authentication
- **PowerShell 7+** (for configuration scripts)

## Prerequisites

### 1. Install .NET 10

Download and install the .NET 10 SDK from https://dotnet.microsoft.com/download.

Verify installation:

```powershell
dotnet --version
```

Must report 10.0.0 or later.

### 2. Install Azure CLI

Download and install from https://learn.microsoft.com/cli/azure/install-azure-cli.

Verify:

```powershell
az --version
```

### 3. Set up Azure authentication

Authenticate with Azure:

```powershell
az login --output none
```

The samples use `AzureCliCredential`. Your `az login` must target the Azure tenant and subscription containing your Foundry project.

Never commit credentials to any repository. Do not paste `az login` output into issues or shared terminals — it may contain account and tenant details.

### 4. Clone the sample repository

```bash
git clone https://github.com/elbruno/mafclaw
cd mafclaw
```

### 5. Build and run a checkpoint

Session 1 provides four incremental checkpoints. Start with the first:

```powershell
cd session-01
dotnet run --project .\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

Or run the finished sample (a copy of Checkpoint 04):

```powershell
dotnet run --project .\code\MafClaw.Session01.csproj
```

See the [Session 1 guide](../../session-01/README.md) for the full checkpoint walkthrough.

## Configuration: Foundry project and model

Sessions 1 and 2 require access to a Microsoft Foundry project with a deployed model (e.g. `gpt-5-mini`). Session 3 is fully offline and needs no Foundry project, model, or network access — see the [Session 3 guide](../../session-03/README.md).

### Configure user secrets

The repository includes a PowerShell setup script. Run from the **repository root**:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

This script:

- Prompts you interactively for Foundry project endpoint and model name.
- Stores settings under keys `Foundry:ProjectEndpoint` and `Foundry:Model` in .NET user-secrets.
- Never stores secrets in files or logs.
- Works only for your current user on this machine.

The `UserSecretsId` is committed to each `.csproj`. All checkpoint projects and the finished sample share one ID — configuring once configures all.

**Preview mode** (run from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -WhatIf
```

**Environment variables** (alternative for CI or scripted use):

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://your-project.services.ai.azure.com/api/projects/your-project"
$env:FOUNDRY_MODEL = "gpt-5-mini"
```

**Clear session settings** (run from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -Clear
```

### Verify configuration

After running the setup script, verify the settings were stored (in a private terminal):

```powershell
dotnet user-secrets list --project .\session-01\checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj
```

### How config is loaded

Each `Program.cs` reads config with three inline lines — no config class:

```csharp
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>().AddEnvironmentVariables().Build();
var endpoint = config["Foundry:ProjectEndpoint"]!;
var model = config["Foundry:Model"] ?? "gpt-5-mini";
```

User-secrets and environment variables are the only two sources. There is no `appsettings.json` and no multi-level configuration hierarchy.

## ⚠️ Privacy warning

These samples have **no try/catch or error-handling wrapper**. Raw Azure exceptions can contain **tenant IDs, account names, resource identifiers, and endpoint URLs**.

If you are streaming, recording, or screen-sharing:

1. **Stop sharing before investigating errors.**
2. Copy the error message to a private window.
3. Never paste raw exception output into issues, chat, or screenshots.

## Troubleshooting

### Not logged in to Azure CLI

**Error:** `Azure.Identity.CredentialUnavailableException` — Azure CLI not logged in or token expired.

**Fix:**

```powershell
az login --output none
```

Ensure you are targeting the correct tenant containing your Foundry project.

### Missing user-secrets

**Error:** `System.NullReferenceException` on `config["Foundry:ProjectEndpoint"]!` or the app crashes immediately.

**Fix:** Run the setup script from the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

### Model not deployed or unavailable

**Error:** `Azure.RequestFailedException` with a 404 or "model not found" message.

**Fix:**

- Confirm the model name in your user-secrets matches a deployed model in your Foundry project. Default is `gpt-5-mini`.
- Check the Foundry portal to verify the deployment exists and your identity has access.
- Do not share the error output — it may contain your project endpoint.

### Content filter or safety refusal

**Error:** `Azure.RequestFailedException` with a 400 and content-filter message.

**Fix:** Respect the refusal. Do not repeatedly rephrase or try to bypass the policy. Choose a clearly benign educational prompt. If the request is still refused, stop.

### Wrong or invalid endpoint

**Error:** `Azure.RequestFailedException` with connection refused, DNS failure, or 401/403.

**Fix:** Re-run `.\tools\configure-user-secrets.ps1 -Session 1`. The endpoint must be an absolute HTTPS URI pointing to your Foundry project.

### Build fails: .NET 10 not found

```powershell
dotnet --list-sdks
```

If .NET 10 does not appear, download from https://dotnet.microsoft.com/download/dotnet/10.0.

### Azure CLI not found

```powershell
az --version
```

If not found, download from https://learn.microsoft.com/cli/azure/install-azure-cli.

## Cost and data-sharing notes

When running live, input prompts and tool results are sent to Azure Foundry. Hosted web search (enabled by default through the Harness) can also send query content and incur charges. Review your data-sharing preferences:

- Read Microsoft's privacy statement: https://privacy.microsoft.com
- Check your Azure subscription's data residency and compliance settings.

**Do not include sensitive personal or financial data in prompts.**

### Mock data

All sample stock quotes are generated for demonstration. They are not real market data and not financial advice.

## Secret hygiene

- **Never commit secrets** to any repository.
- **Never pass secrets** as command-line arguments (visible in process listings and shell history).
- **Use user-secrets only on your local machine** for development and testing.
- **Always redact secrets** when sharing logs or screenshots.

See your organization's security policy for additional guidance.

## Support and feedback

If you encounter issues:

1. Check the troubleshooting section above.
2. Review the session-specific docs in `session-0X/docs/`.
3. Check the official Agent Framework blog series at https://aka.ms/mafclaw/blog.
4. Open an issue: https://aka.ms/mafclaw/repo.

## Keep session-specific content separate

Session-specific setup, troubleshooting, and checkpoint guides live inside each `session-0X/docs/` folder.
