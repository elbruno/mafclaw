# Setup and prerequisites

This guide covers prerequisites, configuration, and troubleshooting for all MafClaw sessions.

## Target stack

- **.NET 10** SDK and runtime
- **C#** (language)
- **Azure Foundry** project and model (for live mode)
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

Verify installation:

```bash
az --version
```

### 3. Set up Azure authentication

To use the live Agent Framework path (not required for offline mode), authenticate with Azure:

```bash
az login --tenant <your-tenant-id>
```

This command authenticates you to Azure CLI. Your credentials are cached locally and accessed by `AzureCliCredential` for live mode.

**Important:** The `az login` command must target the Azure tenant and subscription containing your Foundry project. Verify your tenant and subscription before running commands:

```bash
az account show
```

Your Azure login credential is separate from MafClaw configuration. Never commit credentials to any repository.

### 4. Clone the sample repository

```bash
git clone https://github.com/elbruno/mafclaw
cd mafclaw
```

### 5. Restore and build from a clean checkout

Navigate to the Session 1 code folder and restore packages using the session's NuGet.Config:

```powershell
cd .\session-01\code
dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
dotnet build .\MafClaw.Session01.csproj --configuration Release --no-restore
```

Both commands must complete without errors.

## Configuration: Foundry project and model

MafClaw sessions can run in two modes:

- **Live mode:** Uses your Azure Foundry project and model deployment for real Agent Framework execution.
- **Offline mode:** Runs the deterministic `OfflineClaw` simulation with local
  fixtures and fixed scenario output, suitable for rehearsal and recovery. It is
  not Agent Framework agent or model execution.

### Live mode prerequisites

To use live mode, you need:

1. **Azure subscription** with Foundry service access.
2. **Foundry project endpoint** - the URL to your project.
3. **Foundry model deployment** - the name of the deployed model or specific capability endpoint.

These are not secrets; they are configuration identifiers. However, do not share your project endpoint publicly as it contains tenant information.

### Configure user secrets

The repository includes a PowerShell setup script that configures all required settings. Run it from the **repository root**:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

This script:

- Discovers the Session 1 sample project.
- Prompts you interactively for Foundry project endpoint and model name.
- Securely stores settings under canonical keys `Foundry:ProjectEndpoint` and `Foundry:Model` in your local user-secrets store.
- Never stores secrets in files or logs.
- Works only for your current user on this machine.

The project's `UserSecretsId` (`8f001de5-00b8-4cd2-835b-e0ea21979f0f`) is committed to the `.csproj`. The script never mutates the `.csproj` file and `dotnet user-secrets init` is never needed.

**Preview mode** (recommended first step; run from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -WhatIf
```

Shows which projects and keys would be affected, without reading or writing anything.

**Environment variables** (preferred for CI or scripted use):

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://your-project.services.ai.azure.com/api/projects/your-project"
$env:FOUNDRY_MODEL = "gpt-5.4"
```

The canonical double-underscore forms (`Foundry__ProjectEndpoint`,
`Foundry__Model`) are also accepted. Environment variables are used immediately
without running the script when higher-priority user-secrets and JSON values are
absent.

**Explicit parameters** (convenience, but visible in shell history; run from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 `
  -FoundryProjectEndpoint "https://your-project.services.ai.azure.com/api/projects/your-project" `
  -FoundryModel "gpt-5.4"
```

The script prints a security warning when you use explicit parameters.

**Configure all sessions** (run from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session All
```

Prompts you once for each unique key (shared keys are cached), then applies all sessions.

**Clear session settings** (run from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session 1 -Clear
```

Removes only MafClaw-owned keys for Session 1. All other keys in your user-secrets store are untouched.

### Verify configuration

After running the setup script, verify the settings were stored:

```powershell
dotnet user-secrets list --project .\session-01\code\MafClaw.Session01.csproj
```

This command prints all stored values. Run it only on the same machine where you configured secrets, and in a private terminal.

### Configuration loading order

The app resolves Foundry settings in this effective high-to-low precedence order:

1. .NET user-secrets in Development and other non-Production environments.
2. `appsettings.json` in the process working directory.
3. `appsettings.json` copied beside the built application.
4. Canonical environment variables: `Foundry__ProjectEndpoint` and `Foundry__Model`.
5. Alias environment variables: `FOUNDRY_PROJECT_ENDPOINT` and `FOUNDRY_MODEL`.

Later JSON providers override earlier ones internally, and user-secrets are added
after both JSON providers. Environment variables are explicit lower-priority
fallbacks in this sample rather than configuration-provider overrides.

### Offline mode

No configuration is required for offline mode. Run with `--mode offline` to use deterministic mock data without contacting Azure.

The `--mode` argument is mandatory for all runs. Running without it exits with code 1. There is no automatic fallback to offline mode.

Offline mode is explicitly labeled when active.

## Run a session

Once you've completed setup, navigate to the session folder and start the sample:

```powershell
cd .\session-01\code

# Live mode (requires Foundry configuration)
dotnet run --project .\MafClaw.Session01.csproj -- --mode live

# Offline mode with interactive commands
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline

# Offline mode with deterministic scenario
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario stock
dotnet run --project .\MafClaw.Session01.csproj -- --mode offline --scenario plan
```

**Important:** The `--mode` argument is mandatory. Running without it exits with code 1.

### Expected behavior

- **Live mode:** The agent contacts Foundry, uses real tools, and returns results based on live data and model inference.
- **Offline mode:** The separate `OfflineClaw` simulation uses local mock data and
  deterministic scenario output. It does not construct or run an agent, Harness,
  model, or hosted search.

### Troubleshooting

#### "Missing value for --mode." (Exit code 1)

The `--mode` argument is required. Run with:

```powershell
dotnet run --project .\session-01\code\MafClaw.Session01.csproj -- --mode live
dotnet run --project .\session-01\code\MafClaw.Session01.csproj -- --mode offline
```

#### "Missing required setting 'Foundry:ProjectEndpoint'" (Exit code 2)

Live mode requires configuration. Run the setup script from the repository root:

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

Or set environment variables directly:

```powershell
$env:FOUNDRY_PROJECT_ENDPOINT = "https://your-project.services.ai.azure.com/api/projects/your-project"
$env:FOUNDRY_MODEL = "gpt-5.4"
```

If you don't have Foundry access, use offline mode instead:

```powershell
dotnet run --project .\session-01\code\MafClaw.Session01.csproj -- --mode offline
```

#### "Live mode credential unavailable: Azure.Identity.CredentialUnavailableException" (Exit code 3)

Azure CLI authentication is unavailable or not configured. This means `AzureCliCredential` could not find valid cached credentials. Authenticate with Azure:

```bash
az login --tenant <your-tenant-id>
```

Ensure you are logging into the correct tenant and subscription containing your Foundry project. Verify:

```bash
az account show
```

Then retry the live mode command:

```powershell
dotnet run --project .\session-01\code\MafClaw.Session01.csproj -- --mode live
```

#### "Live mode authentication failed" (Exit code 3)

Your Azure CLI credentials have expired or are no longer valid for your target tenant/subscription. Refresh them:

```bash
az login --tenant <your-tenant-id> --use-device-code
```

Follow the on-screen instructions. Verify you are logged into the correct subscription:

```bash
az account show
```

Then retry the live mode command.

#### "Live mode request failed (4xx or 5xx)" (Exit code 4)

The harness contacted Foundry but received an error. This might indicate:

1. Invalid project endpoint in configuration.
2. Model deployment name is incorrect or not deployed.
3. Foundry service is temporarily unavailable.

Verify your configuration:

```powershell
dotnet user-secrets list --project .\session-01\code\MafClaw.Session01.csproj
```

Reconfigure if needed (from the repository root):

```powershell
.\tools\configure-user-secrets.ps1 -Session 1
```

#### "Live mode network failure" (Exit code 5)

Network connectivity issue. Verify your connection:

```bash
ping -c 4 8.8.8.8
```

If the network is working, the Foundry endpoint may be temporarily unavailable. Try again later.

#### Content-filter or safety refusal

Respect the refusal. Do not repeatedly rephrase the request or use evasive
wording to bypass service policy. Remove unnecessary sensitive content and use a
clearly benign educational prompt; otherwise stop.

#### Build fails: .NET 10 not found

Ensure .NET 10 is installed:

```powershell
dotnet --list-sdks
```

If .NET 10 does not appear, download and install from https://dotnet.microsoft.com/download/dotnet/10.0.

Then rebuild from the session code folder:

```powershell
cd .\session-01\code
dotnet clean .\MafClaw.Session01.csproj
dotnet restore .\MafClaw.Session01.csproj --configfile .\NuGet.Config
dotnet build .\MafClaw.Session01.csproj --no-restore
```

#### "Azure CLI not found"

Verify Azure CLI is installed:

```bash
az --version
```

If not found, download from https://learn.microsoft.com/cli/azure/install-azure-cli.

## Cost and data-sharing notes

### Live mode

When you use live mode, your input prompts and tool results are sent to Azure Foundry for processing by the model. Review your data-sharing preferences:

- Read Microsoft's privacy statement: https://privacy.microsoft.com
- Check your Azure subscription's data residency and compliance settings.
- Understand that model responses may be logged for safety and quality monitoring.

**Do not include sensitive personal or financial data** in prompts when using live mode.

### Mock data

All sample data (stock quotes, news, portfolio files) is generated for demonstration purposes only. It does not represent real market data or real financial advice. Use it only for testing the mechanics of the agent framework.

## Environment aliases (optional)

For convenience, you can create PowerShell aliases to jump between session folders:

```powershell
Set-Alias -Name mafclaw-1 -Value { cd C:\src\mafclaw\session-01\code }
Set-Alias -Name mafclaw-run -Value { dotnet run }
```

Add these to your PowerShell profile if you want them to persist across sessions.

## Secret hygiene

- **Never commit secrets** to any repository.
- **Never pass secrets** as command-line arguments (they are visible in process listings and shell history).
- **Use user-secrets only on your local machine** for development and testing.
- **For CI/CD and shared environments**, use Azure Key Vault or environment variables set by your deployment platform.
- **Always redact secrets** when sharing logs or screenshots.

See your organization's security policy for additional guidance.

## Support and feedback

If you encounter issues:

1. Check the troubleshooting section above.
2. Review the session-specific README in `session-0X/docs`.
3. Check the official Agent Framework documentation and blog series at https://aka.ms/mafclaw/blog.
4. Open an issue on the GitHub repository: https://aka.ms/mafclaw/repo.

## Keep session-specific content separate

Session-specific setup, architecture, prompts, and troubleshooting live inside each `session-0X/docs/README.md` folder.
