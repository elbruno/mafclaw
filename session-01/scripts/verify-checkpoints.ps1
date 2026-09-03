[CmdletBinding()]
param()

Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

$sessionRoot = Split-Path -Parent $PSScriptRoot
$nugetConfig = Join-Path $sessionRoot 'code\NuGet.Config'
$expectedUserSecretsId = '8f001de5-00b8-4cd2-835b-e0ea21979f0f'

$projects = @(
    'code\MafClaw.Session01.csproj'
    'checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj'
    'checkpoints\02-harness-agent\MafClaw.Checkpoint02.csproj'
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

Write-Host 'Checking checkpoint progression contracts...'

$checkpoint01Project = Get-Text 'checkpoints\01-hello-agent\MafClaw.Checkpoint01.csproj'
$checkpoint01Program = Get-Text 'checkpoints\01-hello-agent\Program.cs'
Assert-Match $checkpoint01Program '\.AsAIAgent\(' 'Checkpoint 01 must use AIProjectClient.AsAIAgent.'
Assert-NotMatch $checkpoint01Program 'AsHarnessAgent' 'Checkpoint 01 must not make Harness claims.'
Assert-NotMatch $checkpoint01Project 'Microsoft\.Agents\.AI\.Harness' 'Checkpoint 01 must not reference the Harness package.'
Assert-NotMatch $checkpoint01Program '#pragma' 'Checkpoint 01 must not use #pragma.'
Assert-NotMatch $checkpoint01Program 'try\s*\{' 'Checkpoint 01 must not use try/catch.'
Assert-Match $checkpoint01Project '<NoWarn>OPENAI001;MAAI001</NoWarn>' 'Checkpoint 01 must suppress warnings in the csproj.'

$checkpoint02Program = Get-Text 'checkpoints\02-harness-agent\Program.cs'
$checkpoint02Project = Get-Text 'checkpoints\02-harness-agent\MafClaw.Checkpoint02.csproj'
Assert-Match $checkpoint02Program 'GetResponsesClient\(\)' 'Checkpoint 02 must explicitly create a Responses client.'
Assert-Match $checkpoint02Program 'AsIChatClient\(' 'Checkpoint 02 must explicitly adapt the Responses client to IChatClient.'
Assert-Match $checkpoint02Program 'AsHarnessAgent\(' 'Checkpoint 02 must use AsHarnessAgent.'
Assert-Match $checkpoint02Program 'DisableFileMemory = true' 'Checkpoint 02 must disable file memory.'
Assert-NotMatch $checkpoint02Program '#pragma' 'Checkpoint 02 must not use #pragma.'
Assert-NotMatch $checkpoint02Program 'try\s*\{' 'Checkpoint 02 must not use try/catch.'
Assert-Match $checkpoint02Project '<NoWarn>OPENAI001;MAAI001</NoWarn>' 'Checkpoint 02 must suppress warnings in the csproj.'

$checkpoint03Program = Get-Text 'checkpoints\03-tools-and-search\Program.cs'
$checkpoint03Tools = Get-Text 'checkpoints\03-tools-and-search\StockTools.cs'
$checkpoint03Project = Get-Text 'checkpoints\03-tools-and-search\MafClaw.Checkpoint03.csproj'
Assert-Match $checkpoint03Program 'AsHarnessAgent\(' 'Checkpoint 03 must use AsHarnessAgent.'
Assert-Match $checkpoint03Program 'DisableFileMemory = true' 'Checkpoint 03 must disable file memory.'
Assert-Match $checkpoint03Tools 'get_stock_price' 'Checkpoint 03 must expose get_stock_price.'
Assert-NotMatch $checkpoint03Program '#pragma' 'Checkpoint 03 must not use #pragma.'
Assert-NotMatch $checkpoint03Program 'try\s*\{' 'Checkpoint 03 must not use try/catch.'
Assert-Match $checkpoint03Project '<NoWarn>OPENAI001;MAAI001</NoWarn>' 'Checkpoint 03 must suppress warnings in the csproj.'

$checkpoint04Program = Get-Text 'checkpoints\04-planning-and-todos\Program.cs'
$checkpoint04Tools = Get-Text 'checkpoints\04-planning-and-todos\StockTools.cs'
$checkpoint04Project = Get-Text 'checkpoints\04-planning-and-todos\MafClaw.Checkpoint04.csproj'
Assert-Match $checkpoint04Program 'AsHarnessAgent\(' 'Checkpoint 04 must use AsHarnessAgent.'
Assert-Match $checkpoint04Program 'DisableFileMemory = true' 'Checkpoint 04 must disable file memory.'
Assert-Match $checkpoint04Program 'TodoProvider' 'Checkpoint 04 must use TodoProvider.'
Assert-Match $checkpoint04Program 'CreateSessionAsync' 'Checkpoint 04 must create a session.'
Assert-Match $checkpoint04Tools 'get_stock_price' 'Checkpoint 04 must expose get_stock_price.'
Assert-NotMatch $checkpoint04Program '#pragma' 'Checkpoint 04 must not use #pragma.'
Assert-NotMatch $checkpoint04Program 'try\s*\{' 'Checkpoint 04 must not use try/catch.'
Assert-NotMatch $checkpoint04Program 'DisableAgentModeProvider = true' 'Checkpoint 04 must NOT disable AgentModeProvider.'
Assert-Match $checkpoint04Project '<NoWarn>OPENAI001;MAAI001</NoWarn>' 'Checkpoint 04 must suppress warnings in the csproj.'
foreach ($command in @('/todos', '/exit')) {
    Assert-Match `
        -Text $checkpoint04Program `
        -Pattern ([regex]::Escape($command)) `
        -Message "Checkpoint 04 must expose the $command command."
}

Write-Host 'Checking final Session 1 sample contract...'
$codeRoot = Join-Path $sessionRoot 'code'
$expectedCodeFiles = @(
    'MafClaw.Session01.csproj'
    'NuGet.Config'
    'Program.cs'
    'README.md'
    'StockTools.cs'
)
$actualCodeFiles = Get-ChildItem -LiteralPath $codeRoot -File |
    Select-Object -ExpandProperty Name |
    Sort-Object
$unexpectedCodeFiles = @($actualCodeFiles | Where-Object { $_ -notin $expectedCodeFiles })
$missingCodeFiles = @($expectedCodeFiles | Where-Object { $_ -notin $actualCodeFiles })
if ($unexpectedCodeFiles.Count -gt 0 -or $missingCodeFiles.Count -gt 0) {
    throw "The final sample source file set is stale. Missing: $($missingCodeFiles -join ', '); unexpected: $($unexpectedCodeFiles -join ', ')."
}

$canonicalProjectText = Get-Text 'code\MafClaw.Session01.csproj'
$canonicalProgramText = Get-Text 'code\Program.cs'
$canonicalToolsText = Get-Text 'code\StockTools.cs'
Assert-Match `
    -Text $canonicalProjectText `
    -Pattern '<AssemblyName>MafClaw\.Session01</AssemblyName>' `
    -Message 'The final sample must keep its distinct assembly name.'
Assert-Match `
    -Text $canonicalProjectText `
    -Pattern '<RootNamespace>MafClaw\.Session01</RootNamespace>' `
    -Message 'The final sample must keep its distinct root namespace.'
Assert-Match `
    -Text $canonicalProgramText `
    -Pattern 'AsHarnessAgent\(' `
    -Message 'The final sample must use AsHarnessAgent.'
Assert-Match $canonicalProgramText 'DisableFileMemory = true' 'The final sample must disable file memory.'
Assert-Match $canonicalProgramText 'TodoProvider' 'The final sample must use TodoProvider.'
Assert-Match $canonicalProgramText 'CreateSessionAsync' 'The final sample must create a session.'
Assert-Match $canonicalToolsText 'get_stock_price' 'The final sample must expose get_stock_price.'
Assert-Match $canonicalToolsText 'MSFT.+512\.34m' 'The final sample must retain the mock MSFT price.'
Assert-Match $canonicalToolsText 'NVDA.+184\.72m' 'The final sample must retain the mock NVDA price.'
Assert-NotMatch $canonicalProgramText '#pragma' 'The final sample must not use #pragma.'
Assert-NotMatch $canonicalProgramText 'try\s*\{' 'The final sample must not use try/catch.'
Assert-NotMatch $canonicalProgramText '--mode|offline|fallback' 'The final sample must not reference removed mode or offline behavior.'
Assert-NotMatch $canonicalProjectText 'mock-market-data\.json|appsettings' 'The final sample must not include removed fixture or appsettings files.'
foreach ($removedFile in @(
    'SafeErrors.cs',
    'FoundryConfiguration.cs',
    'ErrorDispatch.cs',
    'AuthErrorFormatter.cs',
    'ClawConsole.cs',
    'PlanningResponse.cs',
    'FinanceAgentFactory.cs',
    'OfflineClaw.cs',
    'mock-market-data.json',
    'appsettings.template.json'
)) {
    if (Test-Path -LiteralPath (Join-Path $codeRoot $removedFile)) {
        throw "The final sample still contains removed file $removedFile."
    }
}

$planningRoot = Split-Path -Parent (Split-Path -Parent $sessionRoot)
$syncScript = Join-Path $planningRoot 'tools\sync.ps1'
if (Test-Path -LiteralPath $syncScript) {
    Write-Host 'Checking the planning sync dry run completes...'
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
}

Write-Host 'Checkpoint verification passed.' -ForegroundColor Green
