<#
.SYNOPSIS
    Configures .NET user-secrets for MafClaw sample projects.

.DESCRIPTION
    The single entry point for setting up .NET user-secrets across all MafClaw
    session sample projects. Discovers .csproj files under each session's code
    folder, runs dotnet user-secrets init when a UserSecretsId is absent, and
    stores the required settings without ever printing configured values.

    Values are resolved in this order for each key:
      1. Explicit parameter — SECURITY: value appears in shell history; prefer env vars
      2. Environment variable (see each parameter's EnvVar name)
      3. Interactive prompt (secrets use masked input; non-secret config uses plain input)

    Use -WhatIf to preview which projects and keys would be affected without
    reading values or writing anything.

    Use -Clear to remove only MafClaw-owned keys for the selected session from the
    user-secrets store. All other keys in the store are untouched.

.PARAMETER Session
    Session to configure: 1, 2, 3, 4, or All. Defaults to 1.

.PARAMETER ProjectPath
    Overrides automatic project discovery with an explicit .csproj path.
    Only valid for single-session runs (not -Session All).

.PARAMETER CodeRoot
    Root directory that contains the session-XX folders. Defaults to the parent
    of the directory that contains this script (i.e., the repository root).
    The private planning-repo wrapper passes the public-staging folder here so
    that both invocation contexts resolve session project paths correctly without
    duplicating logic.

.PARAMETER FoundryProjectEndpoint
    Sets Foundry:ProjectEndpoint. Convenience parameter for CI or scripted local
    setup. Environment variable: FOUNDRY_PROJECT_ENDPOINT.
    SECURITY NOTE: this value will be visible in your shell history.
    Prefer the FOUNDRY_PROJECT_ENDPOINT environment variable or the interactive prompt.

.PARAMETER FoundryModel
    Sets Foundry:Model. Convenience parameter for automation.
    Environment variable: FOUNDRY_MODEL.
    SECURITY NOTE: visible in shell history. Prefer the FOUNDRY_MODEL env var.

.PARAMETER Clear
    Removes only MafClaw-owned keys for the selected session. Safe to combine with
    -WhatIf to preview which keys would be removed.

.EXAMPLE
    .\tools\configure-user-secrets.ps1 -Session 1

.EXAMPLE
    .\tools\configure-user-secrets.ps1 -Session 1 -WhatIf

.EXAMPLE
    .\tools\configure-user-secrets.ps1 -Session All

.EXAMPLE
    # Preferred automation pattern — no shell-history exposure
    $env:FOUNDRY_PROJECT_ENDPOINT = 'https://myproject.services.ai.azure.com/'
    $env:FOUNDRY_MODEL = 'gpt-4o'
    .\tools\configure-user-secrets.ps1 -Session 1

.EXAMPLE
    .\tools\configure-user-secrets.ps1 -Session 1 -Clear

.EXAMPLE
    .\tools\configure-user-secrets.ps1 -Session 1 -Clear -WhatIf
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param (
    [ValidateSet('1', '2', '3', '4', 'All')]
    [string] $Session = '1',

    [string] $ProjectPath,

    # Root directory containing session-XX sub-folders.
    # Defaults to the repository root when invoked from the public repo.
    # The private-repo wrapper overrides this to point at public-staging.
    [string] $CodeRoot,

    # Convenience parameter for automation.
    # SECURITY: the value appears in shell history. Use FOUNDRY_PROJECT_ENDPOINT env var when possible.
    [string] $FoundryProjectEndpoint,

    # Convenience parameter for automation.
    # SECURITY: the value appears in shell history. Use FOUNDRY_MODEL env var when possible.
    [string] $FoundryModel,

    [switch] $Clear
)

Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Session configuration map — non-secret metadata only.
# Each entry declares the keys a session owns so the script can set or clear
# exactly those keys without touching anything else in the user-secrets store.
# Sessions 2-4 are forward-compatible placeholders; their code is not yet
# implemented but their .csproj files can safely receive a UserSecretsId.
# ---------------------------------------------------------------------------
$script:SessionMap = [ordered]@{
    '1' = @{
        Label  = 'Session 1 – Meet Your Agent Harness and Claw'
        Status = 'active'
        Folder = 'session-01'
        Keys   = @(
            [pscustomobject]@{
                Name     = 'Foundry:ProjectEndpoint'
                Prompt   = 'Azure AI Foundry project endpoint URL'
                EnvVar   = 'FOUNDRY_PROJECT_ENDPOINT'
                Required = $true
                IsSecret = $false
            }
            [pscustomobject]@{
                Name     = 'Foundry:Model'
                Prompt   = 'Foundry model or deployment name'
                EnvVar   = 'FOUNDRY_MODEL'
                Required = $true
                IsSecret = $false
            }
        )
    }
    '2' = @{
        Label  = 'Session 2 – Working With Your Data Safely (placeholder – code not yet implemented)'
        Status = 'placeholder'
        Folder = 'session-02'
        Keys   = @(
            [pscustomobject]@{ Name = 'Foundry:ProjectEndpoint'; Prompt = 'Azure AI Foundry project endpoint URL';         EnvVar = 'FOUNDRY_PROJECT_ENDPOINT'; Required = $true;  IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:Model';           Prompt = 'Foundry model or deployment name';             EnvVar = 'FOUNDRY_MODEL';            Required = $true;  IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:MemoryStore';     Prompt = 'Foundry memory store name (Enter to skip)';    EnvVar = 'FOUNDRY_MEMORY_STORE';     Required = $false; IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:EmbeddingModel';  Prompt = 'Foundry embedding model name (Enter to skip)'; EnvVar = 'FOUNDRY_EMBEDDING_MODEL';  Required = $false; IsSecret = $false }
        )
    }
    '3' = @{
        Label  = 'Session 3 – Scaling the Claw or Harness Capabilities (placeholder – code not yet implemented)'
        Status = 'placeholder'
        Folder = 'session-03'
        Keys   = @(
            [pscustomobject]@{ Name = 'Foundry:ProjectEndpoint'; Prompt = 'Azure AI Foundry project endpoint URL';             EnvVar = 'FOUNDRY_PROJECT_ENDPOINT'; Required = $true;  IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:Model';           Prompt = 'Foundry model or deployment name';                 EnvVar = 'FOUNDRY_MODEL';            Required = $true;  IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:MemoryStore';     Prompt = 'Foundry memory store name (Enter to skip)';        EnvVar = 'FOUNDRY_MEMORY_STORE';     Required = $false; IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:EmbeddingModel';  Prompt = 'Foundry embedding model name (Enter to skip)';     EnvVar = 'FOUNDRY_EMBEDDING_MODEL';  Required = $false; IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:ToolboxEndpoint'; Prompt = 'Foundry Toolbox or MCP endpoint (Enter to skip)';  EnvVar = 'FOUNDRY_TOOLBOX_ENDPOINT'; Required = $false; IsSecret = $false }
        )
    }
    '4' = @{
        Label  = 'Session 4 – Making Your Claw Production-Ready (placeholder – code not yet implemented)'
        Status = 'placeholder'
        Folder = 'session-04'
        Keys   = @(
            [pscustomobject]@{ Name = 'Foundry:ProjectEndpoint';              Prompt = 'Azure AI Foundry project endpoint URL';                    EnvVar = 'FOUNDRY_PROJECT_ENDPOINT';              Required = $true;  IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:Model';                        Prompt = 'Foundry model or deployment name';                        EnvVar = 'FOUNDRY_MODEL';                         Required = $true;  IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:MemoryStore';                  Prompt = 'Foundry memory store name (Enter to skip)';               EnvVar = 'FOUNDRY_MEMORY_STORE';                  Required = $false; IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:EmbeddingModel';               Prompt = 'Foundry embedding model name (Enter to skip)';            EnvVar = 'FOUNDRY_EMBEDDING_MODEL';               Required = $false; IsSecret = $false }
            [pscustomobject]@{ Name = 'Foundry:ToolboxEndpoint';              Prompt = 'Foundry Toolbox or MCP endpoint (Enter to skip)';         EnvVar = 'FOUNDRY_TOOLBOX_ENDPOINT';              Required = $false; IsSecret = $false }
            [pscustomobject]@{ Name = 'ApplicationInsights:ConnectionString'; Prompt = 'Application Insights connection string (Enter to skip)';  EnvVar = 'APPLICATIONINSIGHTS_CONNECTION_STRING'; Required = $false; IsSecret = $true  }
            [pscustomobject]@{ Name = 'Foundry:EvaluationEndpoint';           Prompt = 'Foundry evaluation endpoint (Enter to skip)';             EnvVar = 'FOUNDRY_EVALUATION_ENDPOINT';           Required = $false; IsSecret = $false }
        )
    }
}

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Write-Step {
    param([string]$Msg)
    Write-Host "[mafclaw-secrets] $Msg"
}

function Get-SessionCsprojPath {
    param([string]$SessionFolder)
    $codeDir = Join-Path $resolvedCodeRoot $SessionFolder | Join-Path -ChildPath 'code'
    if (-not (Test-Path -LiteralPath $codeDir)) {
        throw "Session code directory not found: $codeDir"
    }
    $csproj = Get-ChildItem -LiteralPath $codeDir -Filter '*.csproj' | Select-Object -First 1
    if (-not $csproj) {
        throw "No .csproj file found in: $codeDir"
    }
    return $csproj.FullName
}

function Test-HasUserSecretsId {
    param([string]$CsprojPath)
    return ((Get-Content -LiteralPath $CsprojPath -Raw) -match '<UserSecretsId>')
}

function Resolve-ConfigValue {
    # Resolves the value for a single key. Never returns a value in WhatIf mode.
    # Caches values in $Cache so -Session All prompts each unique key only once.
    param(
        [pscustomobject] $KeyDef,
        [hashtable]      $Overrides,
        [hashtable]      $Cache
    )

    if ($Cache.ContainsKey($KeyDef.Name)) {
        return $Cache[$KeyDef.Name]
    }

    # Explicit automation parameter (caller has already warned about history risk)
    if ($Overrides.ContainsKey($KeyDef.Name) -and -not [string]::IsNullOrEmpty($Overrides[$KeyDef.Name])) {
        $Cache[$KeyDef.Name] = $Overrides[$KeyDef.Name]
        return $Cache[$KeyDef.Name]
    }

    # Environment variable
    $envVal = [System.Environment]::GetEnvironmentVariable($KeyDef.EnvVar)
    if (-not [string]::IsNullOrEmpty($envVal)) {
        Write-Host "    Source: $($KeyDef.EnvVar) (environment variable)"
        $Cache[$KeyDef.Name] = $envVal
        return $envVal
    }

    # Interactive prompt — use masked input for true secrets
    if ($KeyDef.IsSecret) {
        $secure = Read-Host -Prompt "    $($KeyDef.Prompt)" -AsSecureString
        $bstr   = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try {
            $plain = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
        } finally {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
        $Cache[$KeyDef.Name] = $plain
        return $plain
    }

    $val = Read-Host -Prompt "    $($KeyDef.Prompt)"
    $Cache[$KeyDef.Name] = $val
    return $val
}

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

# Resolve the code root: default is the repository root (parent of the tools folder).
# When called by the private planning-repo wrapper, -CodeRoot is set to public-staging.
$resolvedCodeRoot = if ([string]::IsNullOrEmpty($CodeRoot)) {
    Split-Path -Parent $PSScriptRoot
} else {
    $CodeRoot
}

# Shell-history risk warnings for explicit parameters
$overrides = @{}
if (-not [string]::IsNullOrEmpty($FoundryProjectEndpoint)) {
    Write-Warning 'SECURITY: -FoundryProjectEndpoint is visible in your shell history. Use the FOUNDRY_PROJECT_ENDPOINT environment variable for automation.'
    $overrides['Foundry:ProjectEndpoint'] = $FoundryProjectEndpoint
}
if (-not [string]::IsNullOrEmpty($FoundryModel)) {
    Write-Warning 'SECURITY: -FoundryModel is visible in your shell history. Use the FOUNDRY_MODEL environment variable for automation.'
    $overrides['Foundry:Model'] = $FoundryModel
}

# Detect WhatIf mode. -WhatIf sets $WhatIfPreference to Continue in the local scope.
# We use this to skip interactive prompts (values would not be used anyway).
$isWhatIf = ($WhatIfPreference -ne [System.Management.Automation.ActionPreference]::SilentlyContinue)

# Verify dotnet is available and warn if not .NET 10.
# Skipped in WhatIf mode so dry-runs work on machines without the SDK.
if (-not $isWhatIf) {
    $dotnetVer = (& dotnet --version 2>&1).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet CLI not found. Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'
    }
    if ($dotnetVer -notmatch '^10\.') {
        Write-Warning "dotnet $dotnetVer detected. MafClaw targets .NET 10. Install the .NET 10 SDK if you have not already."
    }
}

$sessionKeys = if ($Session -eq 'All') { [string[]]$script:SessionMap.Keys } else { @($Session) }

# Shared value cache so -Session All collects each unique key only once
$valueCache = @{}

foreach ($sk in $sessionKeys) {
    $cfg = $script:SessionMap[$sk]
    Write-Step $cfg.Label

    # Resolve the target .csproj
    if ($ProjectPath -and $sessionKeys.Count -eq 1) {
        if (-not (Test-Path -LiteralPath $ProjectPath)) {
            throw "Project not found: $ProjectPath"
        }
        $csproj = $ProjectPath
    } else {
        $csproj = Get-SessionCsprojPath -SessionFolder $cfg.Folder
    }

    Write-Host "  Project : $csproj"

    # List key names (never values) for visibility
    if ($Clear) {
        Write-Host '  Keys to remove (MafClaw-owned only):'
    } else {
        Write-Host '  Keys to configure:'
    }
    foreach ($k in $cfg.Keys) {
        $tag = if ($k.Required) { 'required' } else { 'optional' }
        Write-Host "    $($k.Name) [$tag]"
    }
    Write-Host ''

    if ($cfg.Status -eq 'placeholder' -and -not $Clear) {
        Write-Host '  NOTE: Session code is not yet fully implemented.' -ForegroundColor DarkYellow
        Write-Host '  Configuring user-secrets for forward compatibility.' -ForegroundColor DarkYellow
        Write-Host ''
    }

    # ---- UserSecretsId init (set mode only) ----
    if (-not $Clear) {
        if (-not (Test-HasUserSecretsId -CsprojPath $csproj)) {
            if ($PSCmdlet.ShouldProcess($csproj, 'dotnet user-secrets init')) {
                Write-Host '  Initialising user-secrets...'
                $initOut = & dotnet user-secrets init --project $csproj 2>&1
                if ($LASTEXITCODE -ne 0) {
                    throw "dotnet user-secrets init failed for '$csproj': $initOut"
                }
                Write-Host '  UserSecretsId created.'
            }
        } else {
            Write-Host '  UserSecretsId already present — skipping init.'
        }
    }

    # ---- Key operations ----
    foreach ($k in $cfg.Keys) {
        if ($Clear) {
            if ($PSCmdlet.ShouldProcess("$csproj : $($k.Name)", 'Remove user-secret')) {
                $rmOut = & dotnet user-secrets remove $k.Name --project $csproj 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  Removed  : $($k.Name)"
                } else {
                    Write-Host "  Not found: $($k.Name) (nothing to remove)"
                }
            }
        } else {
            # Collect value only when not in WhatIf mode (no interactive prompts for dry runs)
            $value = $null
            if (-not $isWhatIf) {
                $value = Resolve-ConfigValue -KeyDef $k -Overrides $overrides -Cache $valueCache

                if ([string]::IsNullOrEmpty($value)) {
                    if ($k.Required) {
                        throw "Required value for '$($k.Name)' was not provided."
                    }
                    Write-Host "  Skipped  : $($k.Name) (optional, no value supplied)"
                    continue
                }
            }

            # ShouldProcess prints the WhatIf message and returns $false in WhatIf mode.
            # In normal mode it returns $true and the set runs.
            if ($PSCmdlet.ShouldProcess("$csproj : $($k.Name)", 'Set user-secret')) {
                $setOut = & dotnet user-secrets set $k.Name $value --project $csproj 2>&1
                if ($LASTEXITCODE -ne 0) {
                    throw "dotnet user-secrets set failed for '$($k.Name)': $setOut"
                }
                Write-Host "  Set      : $($k.Name)"
            }
        }
    }

    Write-Host ''
}

# Summary
if ($isWhatIf) {
    Write-Host 'WhatIf complete — no changes were made.' -ForegroundColor Yellow
} elseif ($Clear) {
    Write-Host 'Clear complete.' -ForegroundColor Green
    Write-Host 'Run without -Clear to reconfigure the removed keys.'
} else {
    Write-Host 'Configuration complete.' -ForegroundColor Green
    Write-Host "Verify: dotnet user-secrets list --project <path>"
    Write-Host '(Run in a private terminal — the list command prints stored values.)'
}
