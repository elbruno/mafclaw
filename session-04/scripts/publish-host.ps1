<#
.SYNOPSIS
Produces a fresh Linux .NET 10 hosted bundle; does not deploy or change Azure.
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$hostProject = Join-Path $root 'code\Hosted\MafClaw.Session04.Hosted.csproj'
$output = Join-Path $root ('.local\publish\hosted-' + [guid]::NewGuid().ToString('N'))
dotnet publish $hostProject -c Release -r linux-x64 --self-contained false -o $output --nologo
if ($LASTEXITCODE -ne 0) { throw 'Hosted publish failed. No deployment was attempted.' }
if (-not (Test-Path (Join-Path $output 'MafClaw.Session04.Hosted.dll'))) { throw 'Hosted entry point is missing.' }
Write-Host "Fresh hosted bundle: $output"
Write-Host 'No Azure resource was created and no image or agent was deployed.'
