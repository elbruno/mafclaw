<#
.SYNOPSIS
Discovers, builds and verifies every public MafClaw project.
.DESCRIPTION
Offline is the default. Live and All explicitly permit configured live calls.
Manifests contain only public synthetic fixtures, never configuration values.
Reports exclude captured stdout/stderr because live diagnostics can contain identifiers.
Inventory reports missing coverage without building. Other modes require every
runnable project to be covered directly or through a manifest's coversProjects.
#>
[CmdletBinding()]
param(
    [string]$CodeRoot = (Split-Path -Parent $PSScriptRoot),
    [ValidateSet('Inventory', 'Build', 'Offline', 'Live', 'All')][string]$Mode = 'Offline',
    [string[]]$CaseId = @(),
    [string]$ReportPath,
    [ValidateRange(1, 1800)][int]$BuildTimeoutSeconds = 600
)

Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RepositoryVerification.psm1') -Force
$root = [IO.Path]::GetFullPath($CodeRoot)
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $root '.local\verification\repository-results.json'
}
$reportFile = [IO.Path]::GetFullPath($ReportPath)
$report = [ordered]@{
    schemaVersion = 1
    createdUtc = [DateTimeOffset]::UtcNow.ToString('o')
    mode = $Mode
    requestedCases = @($CaseId)
    status = 'Fail'
    projects = @()
    builds = @()
    cases = @()
    failures = @()
}
$failures = [Collections.Generic.List[string]]::new()
$builds = [Collections.Generic.List[object]]::new()
$results = [Collections.Generic.List[object]]::new()
try {
    $projects = @(Get-MafClawProjectInventory -CodeRoot $root)
    if ($projects.Count -eq 0) { throw 'No source projects were discovered.' }
    $cases = @(Get-MafClawVerificationCases -CodeRoot $root)
    $knownCaseIds = @($cases | ForEach-Object { $_.Id })
    foreach ($requested in $CaseId) {
        if ($requested -notin $knownCaseIds) { throw "Unknown verification case: $requested" }
    }
    $known = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $covered = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($project in $projects) { [void]$known.Add($project.Path) }
    foreach ($case in $cases) {
        foreach ($path in $case.CoveredProjects) {
            if (-not $known.Contains($path)) { throw "Case $($case.Id) covers a project outside the inventory." }
            [void]$covered.Add($path)
        }
    }
    $report.projects = @(
        foreach ($project in $projects) {
            $validFramework = $project.Frameworks.Count -gt 0 -and
                @($project.Frameworks | Where-Object { $_ -ne 'net10.0' }).Count -eq 0
            if (-not $validFramework) { $failures.Add("Expected net10.0: $($project.RelativePath)") }
            $hasCoverage = -not $project.IsRunnable -or $covered.Contains($project.Path)
            if (-not $hasCoverage -and $Mode -ne 'Inventory') {
                $failures.Add("No verification case covers runnable project: $($project.RelativePath)")
            }
            [pscustomobject]@{
                project = $project.RelativePath; frameworks = $project.Frameworks
                runnable = $project.IsRunnable; hasVerificationCase = $hasCoverage
                packages = $project.Packages
            }
        }
    )
    if ($Mode -ne 'Inventory') {
        foreach ($project in $projects) {
            $result = Invoke-MafClawProcess -FilePath 'dotnet' -WorkingDirectory $root `
                -Arguments @('build', $project.Path, '-c', 'Release', '--nologo', '--verbosity', 'quiet') `
                -TimeoutSeconds $BuildTimeoutSeconds
            $status = if (-not $result.TimedOut -and $result.ExitCode -eq 0) { 'Pass' } else { 'Fail' }
            $builds.Add([pscustomobject]@{
                project = $project.RelativePath; status = $status
                exitCode = $result.ExitCode; timedOut = $result.TimedOut
                durationMilliseconds = $result.DurationMilliseconds
            })
            Write-Host "Build ${status}: $($project.RelativePath)"
            if ($status -eq 'Fail') {
                $failures.Add("Build failed: $($project.RelativePath). Rerun privately for diagnostics.")
            }
        }
    }
    foreach ($case in $cases) {
        $selected = $Mode -eq 'All' -or
            ($Mode -eq 'Offline' -and $case.Mode -eq 'offline') -or
            ($Mode -eq 'Live' -and $case.Mode -eq 'live')
        if ($CaseId.Count -gt 0 -and $case.Id -notin $CaseId) { $selected = $false }
        if (-not $selected) {
            $results.Add([pscustomobject]@{ id = $case.Id; mode = $case.Mode; status = 'NotRun'; reason = 'Not selected by mode/case filter.' })
            continue
        }
        $failedBuild = @($builds | Where-Object {
            $_.status -eq 'Fail' -and (Join-Path $root $_.project) -in $case.CoveredProjects
        }).Count -gt 0
        if ($failedBuild) {
            $results.Add([pscustomobject]@{ id = $case.Id; mode = $case.Mode; status = 'Blocked'; reason = 'Required build failed.' })
            continue
        }
        $arguments = if ($case.Runner -eq 'test') {
            @('test', $case.Project, '-c', 'Release', '--no-build', '--no-restore', '--nologo') + $case.Arguments
        } else {
            @('run', '--project', $case.Project, '-c', 'Release', '--no-build', '--no-restore', '--') + $case.Arguments
        }
        $process = Invoke-MafClawProcess -FilePath 'dotnet' -Arguments $arguments `
            -WorkingDirectory $case.WorkingDirectory -InputText $case.InputText `
            -EnvironmentOverrides $case.Environment -TimeoutSeconds $case.TimeoutSeconds
        $result = Test-MafClawCaseResult -Case $case -ProcessResult $process
        $results.Add($result)
        Write-Host "Case $($result.Status): $($case.Id)"
        if ($result.Status -eq 'Fail') {
            foreach ($failure in $result.Failures) { $failures.Add("$($case.Id): $failure") }
        }
    }
    $report.status = if ($failures.Count -eq 0) { 'Pass' } else { 'Fail' }
}
catch {
    $failures.Add($_.Exception.Message)
}
finally {
    $report.builds = @($builds)
    $report.cases = @($results)
    $report.failures = @($failures)
    [IO.Directory]::CreateDirectory((Split-Path -Parent $reportFile)) | Out-Null
    [IO.File]::WriteAllText($reportFile, ($report | ConvertTo-Json -Depth 12))
}
if ($report.status -eq 'Fail') {
    foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
    throw "Repository verification failed. Safe report: $reportFile"
}
Write-Host "Repository verification passed for mode $Mode. Unselected live cases are NotRun, not verified."
Write-Host "Safe report: $reportFile"
