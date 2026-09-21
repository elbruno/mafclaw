<#
.SYNOPSIS
Builds and verifies the complete Session 4 package with explicit offline/live selection.
#>
[CmdletBinding()]
param(
    [ValidateSet('Inventory', 'Build', 'Offline', 'Live', 'All')][string]$Mode = 'Offline',
    [string[]]$CaseId = @(),
    [string]$ReportPath
)
$ErrorActionPreference = 'Stop'
$sessionRoot = Split-Path -Parent $PSScriptRoot
$verify = Join-Path $sessionRoot '..\tools\verify-repository.ps1'
& $verify -CodeRoot $sessionRoot @PSBoundParameters
