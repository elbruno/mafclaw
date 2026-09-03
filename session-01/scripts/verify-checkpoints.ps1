[CmdletBinding()]
param()

Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

$sessionRoot = Split-Path -Parent $PSScriptRoot
$nugetConfig = Join-Path $sessionRoot 'code\NuGet.Config'
$expectedUserSecretsId = '8f001de5-00b8-4cd2-835b-e0ea21979f0f'

$projects = @(
    'code\MafClaw.Session01.csproj'
    'checkpoints\01-standard-agent\MafClaw.Checkpoint01.csproj'
    'checkpoints\02-harness-core\MafClaw.Checkpoint02.csproj'
    'checkpoints\03-tools-and-search\MafClaw.Checkpoint03.csproj'
    'checkpoints\04-planning-and-todos\MafClaw.Checkpoint04.csproj'
)

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    Write-Host "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Get-Text {
    param(
        [Parameter(Mandatory)]
        [string] $RelativePath
    )

    return Get-Content -LiteralPath (Join-Path $sessionRoot $RelativePath) -Raw
}

function Assert-Match {
    param(
        [Parameter(Mandatory)]
        [string] $Text,

        [Parameter(Mandatory)]
        [string] $Pattern,

        [Parameter(Mandatory)]
        [string] $Message
    )

    if ($Text -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-NotMatch {
    param(
        [Parameter(Mandatory)]
        [string] $Text,

        [Parameter(Mandatory)]
        [string] $Pattern,

        [Parameter(Mandatory)]
        [string] $Message
    )

    if ($Text -match $Pattern) {
        throw $Message
    }
}

function Test-MissingConfiguration {
    param(
        [Parameter(Mandatory)]
        [string] $AssemblyPath
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.WorkingDirectory = $sessionRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add($AssemblyPath)
    $startInfo.Environment['DOTNET_ENVIRONMENT'] = 'Production'

    foreach ($key in @(
        'Foundry__ProjectEndpoint'
        'Foundry__Model'
        'FOUNDRY_PROJECT_ENDPOINT'
        'FOUNDRY_MODEL'
    )) {
        [void] $startInfo.Environment.Remove($key)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if ($process.ExitCode -ne 2) {
        throw "$AssemblyPath missing-configuration gate returned exit code $($process.ExitCode), expected 2."
    }

    Assert-Match `
        -Text $standardError `
        -Pattern '^Missing Foundry:ProjectEndpoint\.' `
        -Message "$AssemblyPath must return safe missing-configuration guidance."

    Assert-NotMatch `
        -Text "$standardOutput`n$standardError" `
        -Pattern '(?m)^\s*at\s|Exception:|Stack trace:' `
        -Message "$AssemblyPath exposed raw exception details."
}

Write-Host 'Restoring and building Session 1 projects with warnings as errors...'
foreach ($relativeProject in $projects) {
    $project = Join-Path $sessionRoot $relativeProject
    Invoke-DotNet @(
        'restore'
        $project
        '--configfile'
        $nugetConfig
        '/warnaserror'
    )
    Invoke-DotNet @(
        'build'
        $project
        '-c'
        'Release'
        '--no-restore'
        '/warnaserror'
    )

    $projectText = Get-Content -LiteralPath $project -Raw
    Assert-Match `
        -Text $projectText `
        -Pattern "<UserSecretsId>$([regex]::Escape($expectedUserSecretsId))</UserSecretsId>" `
        -Message "$relativeProject must use the stable Session 1 UserSecretsId."
}

Write-Host 'Checking safe, non-network startup failures without configuration...'
foreach ($checkpointAssembly in @(
    'checkpoints\01-standard-agent\bin\Release\net10.0\MafClaw.Checkpoint01.dll'
    'checkpoints\02-harness-core\bin\Release\net10.0\MafClaw.Checkpoint02.dll'
    'checkpoints\03-tools-and-search\bin\Release\net10.0\MafClaw.Checkpoint03.dll'
    'checkpoints\04-planning-and-todos\bin\Release\net10.0\MafClaw.Checkpoint04.dll'
)) {
    Test-MissingConfiguration (Join-Path $sessionRoot $checkpointAssembly)
}

Write-Host 'Checking checkpoint progression contracts...'

$configurationFiles = @(
    'code\FoundryConfiguration.cs'
    'checkpoints\01-standard-agent\FoundryConfiguration.cs'
    'checkpoints\02-harness-core\FoundryConfiguration.cs'
    'checkpoints\03-tools-and-search\FoundryConfiguration.cs'
    'checkpoints\04-planning-and-todos\FoundryConfiguration.cs'
)
foreach ($configurationFile in $configurationFiles) {
    $configurationText = Get-Text $configurationFile
    Assert-Match `
        -Text $configurationText `
        -Pattern '(?s)Path\.Combine\(AppContext\.BaseDirectory,\s*"appsettings\.json"\).*Path\.Combine\(Directory\.GetCurrentDirectory\(\),\s*"appsettings\.json"\).*AddUserSecrets' `
        -Message "$configurationFile must load output JSON, then working-directory JSON, then user-secrets."
}

$canonicalConfiguration = Get-Text 'code\FoundryConfiguration.cs'
Assert-Match `
    -Text $canonicalConfiguration `
    -Pattern '(?s)ReadNonEmpty\(configValues,\s*EndpointKey\).*ReadNonEmpty\(environmentLookup,\s*EndpointCanonicalEnvironmentKey\).*ReadNonEmpty\(environmentLookup,\s*EndpointAlias\)' `
    -Message 'The final sample endpoint precedence must be configuration, canonical environment, then alias.'
Assert-Match `
    -Text $canonicalConfiguration `
    -Pattern '(?s)ReadNonEmpty\(configValues,\s*ModelKey\).*ReadNonEmpty\(environmentLookup,\s*ModelCanonicalEnvironmentKey\).*ReadNonEmpty\(environmentLookup,\s*ModelAlias\)' `
    -Message 'The final sample model precedence must be configuration, canonical environment, then alias.'

$checkpoint01Project = Get-Text 'checkpoints\01-standard-agent\MafClaw.Checkpoint01.csproj'
$checkpoint01Program = Get-Text 'checkpoints\01-standard-agent\Program.cs'
Assert-Match $checkpoint01Program '\.AsAIAgent\(' 'Checkpoint 01 must use AIProjectClient.AsAIAgent.'
Assert-Match $checkpoint01Program 'LIVE · checkpoint 01 · standard agent' 'Checkpoint 01 must label live output.'
Assert-NotMatch $checkpoint01Program 'AsHarnessAgent' 'Checkpoint 01 must not make Harness claims.'
Assert-NotMatch $checkpoint01Project 'Microsoft\.Agents\.AI\.Harness' 'Checkpoint 01 must not reference the Harness package.'

$checkpoint02Program = Get-Text 'checkpoints\02-harness-core\Program.cs'
Assert-Match $checkpoint02Program 'GetResponsesClient\(\)' 'Checkpoint 02 must explicitly create a Responses client.'
Assert-Match $checkpoint02Program 'AsIChatClient\(' 'Checkpoint 02 must explicitly adapt the Responses client to IChatClient.'
Assert-Match $checkpoint02Program 'AsHarnessAgent\(' 'Checkpoint 02 must use AsHarnessAgent.'
Assert-Match $checkpoint02Program 'LIVE · checkpoint 02 · harness core' 'Checkpoint 02 must label live output.'
foreach ($disabledFeature in @(
    'DisableFileMemory = true'
    'DisableWebSearch = true'
    'DisableTodoProvider = true'
    'DisableAgentModeProvider = true'
    'DisableAgentSkillsProvider = true'
)) {
    Assert-Match `
        -Text $checkpoint02Program `
        -Pattern ([regex]::Escape($disabledFeature)) `
        -Message "Checkpoint 02 must explicitly set '$disabledFeature'."
}

$checkpoint03Program = Get-Text 'checkpoints\03-tools-and-search\Program.cs'
$checkpoint03Tools = Get-Text 'checkpoints\03-tools-and-search\StockTools.cs'
Assert-Match $checkpoint03Program 'DisableWebSearch = false' 'Checkpoint 03 must enable Harness-hosted web search.'
Assert-Match $checkpoint03Program 'recent NVDA news' 'Checkpoint 03 must demonstrate the NVDA news path.'
Assert-Match $checkpoint03Program 'illustrative MSFT price' 'Checkpoint 03 must demonstrate the MSFT quote path.'
Assert-Match $checkpoint03Program 'LIVE · checkpoint 03 · tools and search' 'Checkpoint 03 must label live output.'
Assert-Match $checkpoint03Program '\[Hosted web search was used\.\]' 'Checkpoint 03 must use the precise search-used marker.'
Assert-NotMatch $checkpoint03Program 'citations are supplied by the model response' 'Checkpoint 03 must not guarantee citations from the search marker.'
Assert-Match $checkpoint03Tools '"get_stock_price"' 'Checkpoint 03 must expose get_stock_price.'

$checkpoint04Factory = Get-Text 'checkpoints\04-planning-and-todos\FinanceAgentFactory.cs'
$checkpoint04Console = Get-Text 'checkpoints\04-planning-and-todos\ClawConsole.cs'
Assert-Match $checkpoint04Factory 'TodoProvider' 'Checkpoint 04 must configure and provide a TodoProvider instance.'
Assert-Match $checkpoint04Factory 'DisableAgentModeProvider = true' 'Checkpoint 04 must keep AgentModeProvider disabled.'
Assert-Match $checkpoint04Factory 'DisableWebSearch = false' 'Checkpoint 04 must keep hosted web search enabled.'
Assert-Match $checkpoint04Console 'ChatResponseFormat\.ForJsonSchema<PlanningResponse>' 'Checkpoint 04 must use structured planning.'
Assert-Match $checkpoint04Console 'LIVE · mafclaw · checkpoint 04 · planning and todos' 'Checkpoint 04 must label live output.'
Assert-Match $checkpoint04Console '\[Hosted web search was used\.\]' 'Checkpoint 04 must use the precise search-used marker.'
foreach ($command in @('/mode', '/todos', '/exit')) {
    Assert-Match `
        -Text $checkpoint04Console `
        -Pattern ([regex]::Escape($command)) `
        -Message "Checkpoint 04 must expose the $command command."
}

$fixturePath = Join-Path $sessionRoot 'shared\mock-market-data.json'
$compatibilityFixturePath = Join-Path $sessionRoot 'code\mock-market-data.json'
if (-not (Test-Path -LiteralPath $compatibilityFixturePath)) {
    throw 'The final sample compatibility fixture is missing.'
}

$fixtureHash = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash
$compatibilityFixtureHash =
    (Get-FileHash -LiteralPath $compatibilityFixturePath -Algorithm SHA256).Hash
if ($fixtureHash -ne $compatibilityFixtureHash) {
    throw 'The shared and final-sample compatibility fixtures must remain identical.'
}

$canonicalProjectText = Get-Text 'code\MafClaw.Session01.csproj'
$canonicalConsoleText = Get-Text 'code\ClawConsole.cs'
$canonicalProgramText = Get-Text 'code\Program.cs'
Assert-Match `
    -Text $canonicalProjectText `
    -Pattern '<None Update="mock-market-data\.json">' `
    -Message 'The final sample must use its unambiguous local compatibility fixture.'
Assert-NotMatch `
    -Text $canonicalProjectText `
    -Pattern '<None Include="\.\.\\shared\\mock-market-data\.json"' `
    -Message 'The final sample must not link the checkpoint canonical fixture.'
Assert-Match `
    -Text $canonicalConsoleText `
    -Pattern 'LIVE · mafclaw · Session 01' `
    -Message 'The final sample must label live output.'
Assert-Match `
    -Text $canonicalConsoleText `
    -Pattern '\[Hosted web search was used\.\]' `
    -Message 'The final sample must use the precise search-used marker.'
Assert-Match `
    -Text $canonicalProgramText `
    -Pattern 'TryCreateStockTools' `
    -Message 'The final sample must sanitize fixture initialization failures.'

$marketData = Get-Content -LiteralPath $fixturePath -Raw | ConvertFrom-Json
$msft = $marketData | Where-Object symbol -eq 'MSFT'
$nvda = $marketData | Where-Object symbol -eq 'NVDA'
if ($msft.price -ne 512.34 -or $nvda.price -ne 184.72) {
    throw 'The shared deterministic market fixture changed unexpectedly.'
}

$planningRoot = Split-Path -Parent (Split-Path -Parent $sessionRoot)
$syncScript = Join-Path $planningRoot 'tools\sync.ps1'
if (Test-Path -LiteralPath $syncScript) {
    Write-Host 'Checking the planning sync dry run preserves the compatibility fixture...'
    $syncOutput = & pwsh -NoProfile -File $syncScript -WhatIf 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "The planning sync WhatIf failed with exit code $LASTEXITCODE."
    }

    $syncText = $syncOutput | Out-String

    Assert-Match `
        -Text $syncText `
        -Pattern 'No files were written\.' `
        -Message 'The planning sync WhatIf did not complete successfully.'

    $deleteSection = [regex]::Match(
        $syncText,
        'Files to DELETE[\s\S]*?WhatIf summary:')
    if (-not $deleteSection.Success) {
        throw 'The planning sync WhatIf deletion manifest was not found.'
    }

    Assert-NotMatch `
        -Text $deleteSection.Value `
        -Pattern ([regex]::Escape('session-01\code\mock-market-data.json')) `
        -Message 'The planning sync WhatIf must not delete the final compatibility fixture.'
}

Write-Host 'Running canonical offline smoke scenarios...'
$canonicalProject = Join-Path $sessionRoot 'code\MafClaw.Session01.csproj'
foreach ($scenario in @('stock', 'plan')) {
    $output = & dotnet run `
        --project $canonicalProject `
        -c Release `
        --no-build `
        -- `
        --mode offline `
        --scenario $scenario 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "Canonical offline $scenario scenario failed with exit code $LASTEXITCODE."
    }

    $outputText = $output -join [Environment]::NewLine
    Assert-Match $outputText 'OFFLINE FALLBACK' "Offline $scenario smoke must be explicitly labeled."
    Assert-Match `
        -Text $outputText `
        -Pattern "SCENARIO $scenario" `
        -Message "Offline $scenario smoke did not run the requested scenario."
}

Write-Host 'Checkpoint verification passed.' -ForegroundColor Green
