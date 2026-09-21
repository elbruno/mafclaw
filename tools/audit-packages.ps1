<#
.SYNOPSIS
Records current, latest and transitive NuGet versions after restoring projects.
.DESCRIPTION
Queries public nuget.org metadata. Existing preview dependencies are compared
with current previews; stable dependencies are compared with stable releases.
Outdated direct dependencies and unavailable audits fail. Outdated transitives
are reported for review instead of being forcibly pinned.
#>
[CmdletBinding()]
param(
    [string]$CodeRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$ReportPath,
    [switch]$IncludeAdvisories
)
Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'RepositoryVerification.psm1') -Force
$root = [IO.Path]::GetFullPath($CodeRoot)
if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $root '.local\verification\package-audit.json'
}
$report = [ordered]@{
    schemaVersion = 1
    checkedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    source = 'https://api.nuget.org/v3/index.json'
    status = 'Fail'
    packages = @()
    advisories = @()
    failures = @()
}
$failures = [Collections.Generic.List[string]]::new()
$rows = [Collections.Generic.List[object]]::new()
$advisories = [Collections.Generic.List[object]]::new()
try {
    $projects = @(Get-MafClawProjectInventory -CodeRoot $root)
    if ($projects.Count -eq 0) { throw 'No source projects were discovered.' }
    $references = @{}
    foreach ($project in $projects) {
        if (-not (Test-Path -LiteralPath $project.AssetsPath -PathType Leaf)) {
            $failures.Add("Restore is required before package audit: $($project.RelativePath)")
            continue
        }
        $assets = Get-Content -LiteralPath $project.AssetsPath -Raw | ConvertFrom-Json -AsHashtable
        foreach ($library in $assets.libraries.GetEnumerator()) {
            if ($library.Value.type -ne 'package') { continue }
            $id, $version = $library.Key -split '/', 2
            $key = "$id/$version"
            if (-not $references.ContainsKey($key)) {
                $references[$key] = @{
                    Id = $id; Version = $version
                    DirectProjects = [Collections.Generic.List[string]]::new()
                    TransitiveProjects = [Collections.Generic.List[string]]::new()
                }
            }
            if (@($project.Packages | Where-Object { $_.Id -eq $id }).Count -gt 0) {
                $references[$key].DirectProjects.Add($project.RelativePath)
            } else {
                $references[$key].TransitiveProjects.Add($project.RelativePath)
            }
        }
        if ($IncludeAdvisories) {
            foreach ($kind in @('vulnerable', 'deprecated')) {
                $result = Invoke-MafClawProcess -FilePath 'dotnet' -WorkingDirectory $root `
                    -Arguments @('package', 'list', '--project', $project.Path, '--no-restore',
                        "--$kind", '--include-transitive', '--format', 'json',
                        '--source', 'https://api.nuget.org/v3/index.json') -TimeoutSeconds 180
                if ($result.TimedOut -or $result.ExitCode -ne 0) {
                    $failures.Add("$kind lookup unavailable: $($project.RelativePath)")
                    continue
                }
                $data = $result.StandardOutput | ConvertFrom-Json -AsHashtable
                foreach ($entry in $data.projects) {
                    foreach ($framework in @($entry['frameworks'])) {
                        if ($null -eq $framework) { continue }
                        foreach ($package in @($framework['topLevelPackages']) + @($framework['transitivePackages'])) {
                            if ($null -eq $package) { continue }
                            $advisories.Add([pscustomobject]@{
                                project = $project.RelativePath; kind = $kind
                                package = $package.id; version = $package.resolvedVersion
                                vulnerabilities = $package['vulnerabilities']
                                deprecationReasons = $package['deprecationReasons']
                                alternativePackage = $package['alternativePackage']
                            })
                            $failures.Add("$kind package requires resolution: $($package.id) in $($project.RelativePath)")
                        }
                    }
                }
            }
        }
    }
    $cache = @{}
    foreach ($entry in $references.Values | Sort-Object Id, Version) {
        if (-not $cache.ContainsKey($entry.Id)) {
            $uri = 'https://api.nuget.org/v3-flatcontainer/' + $entry.Id.ToLowerInvariant() + '/index.json'
            try {
                $cache[$entry.Id] = @((
                    Invoke-RestMethod -Uri $uri -TimeoutSec 30 -MaximumRetryCount 1
                ).versions)
            }
            catch {
                $failures.Add("Version lookup unavailable for $($entry.Id); no latest-version claim can be made.")
                $cache[$entry.Id] = @()
            }
        }
        $versions = @($cache[$entry.Id])
        $candidates = if ($entry.Version -match '-') {
            $versions
        } else {
            @($versions | Where-Object { $_ -notmatch '-' })
        }
        $latest = $candidates | Select-Object -Last 1
        $status = if ($null -eq $latest) { 'Unavailable' } elseif ($entry.Version -eq $latest) {
            'Current'
        } else { 'Review' }
        if ($status -eq 'Review' -and $entry.DirectProjects.Count -gt 0) {
            $failures.Add("Direct dependency update required: $($entry.Id) $($entry.Version) -> $latest")
        }
        $rows.Add([pscustomobject]@{
            id = $entry.Id; resolvedVersion = $entry.Version; latestApplicableVersion = $latest
            channel = if ($entry.Version -match '-') { 'Preview' } else { 'Stable' }
            status = $status
            directProjects = @($entry.DirectProjects)
            transitiveProjects = @($entry.TransitiveProjects)
        })
    }
    $report.status = if ($failures.Count -eq 0) { 'Pass' } else { 'Fail' }
}
catch {
    $failures.Add($_.Exception.Message)
}
finally {
    $report.packages = @($rows)
    $report.advisories = @($advisories)
    $report.failures = @($failures)
    $path = [IO.Path]::GetFullPath($ReportPath)
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    [IO.File]::WriteAllText($path, ($report | ConvertTo-Json -Depth 15))
}
foreach ($failure in $failures) { Write-Host "FAIL: $failure" }
if ($report.status -eq 'Fail') { throw "Package audit incomplete or failed. Safe report: $path" }
Write-Host "Package audit passed for direct dependencies. Review transitive candidates in: $path"
if (-not $IncludeAdvisories) { Write-Host 'Vulnerability/deprecation checks were not selected.' }
