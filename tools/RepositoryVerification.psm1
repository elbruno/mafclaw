Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

function Invoke-MafClawProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [ValidateLength(0, 1048576)][string]$InputText = '',
        [hashtable]$EnvironmentOverrides = @{},
        [ValidateRange(1, 1800)][int]$TimeoutSeconds = 180
    )

    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $FilePath
    $start.WorkingDirectory = $WorkingDirectory
    $start.UseShellExecute = $false
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    foreach ($name in $EnvironmentOverrides.Keys) { $start.Environment[$name] = [string]$EnvironmentOverrides[$name] }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $start
    $clock = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $process.Start()) { throw 'Verification child process did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $timedOut = $false
        $inputTask = $null
        try {
            if ($InputText.Length -gt 0) {
                $inputTask = $process.StandardInput.WriteAsync($InputText)
                [void]($inputTask.WaitAsync([TimeSpan]::FromSeconds($TimeoutSeconds)).GetAwaiter().GetResult())
            }
        }
        catch [TimeoutException] {
            $timedOut = $true
        }
        catch [IO.IOException] {
            # An early child exit can close stdin before the fixture is delivered.
            if (-not $process.HasExited) { throw }
        }
        finally {
            if (-not $timedOut) { $process.StandardInput.Close() }
        }
        if (-not $timedOut) {
            $remaining = [int][Math]::Max(0, $TimeoutSeconds * 1000 - $clock.ElapsedMilliseconds)
            $timedOut = -not $process.WaitForExit($remaining)
        }
        if ($timedOut) {
            try { $process.Kill($true) }
            catch [InvalidOperationException] {
                if (-not $process.HasExited) { throw }
            }
            if (-not $process.WaitForExit(10000)) {
                throw 'The timed-out verification process did not terminate.'
            }
            if ($null -ne $inputTask) {
                try { [void]($inputTask.WaitAsync([TimeSpan]::FromSeconds(10)).GetAwaiter().GetResult()) }
                catch [IO.IOException] {
                    if (-not $process.HasExited) { throw }
                }
                $process.StandardInput.Close()
            }
        }
        # Bound collection too: a descendant must not hold redirected pipes forever.
        if (-not [Threading.Tasks.Task]::WaitAll(
            [Threading.Tasks.Task[]]@($stdout, $stderr), 10000)) {
            throw 'Verification output collection exceeded its shutdown deadline.'
        }
        [pscustomobject]@{
            ExitCode = $process.ExitCode
            TimedOut = $timedOut
            DurationMilliseconds = $clock.ElapsedMilliseconds
            StandardOutput = $stdout.GetAwaiter().GetResult()
            StandardError = $stderr.GetAwaiter().GetResult()
        }
    }
    finally {
        $clock.Stop()
        $process.Dispose()
    }
}

function Resolve-MafClawProjectPath {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$RelativePath
    )
    if ([IO.Path]::IsPathRooted($RelativePath)) {
        throw 'Manifest project paths must be relative.'
    }
    $rootPath = [IO.Path]::GetFullPath($Root)
    $path = [IO.Path]::GetFullPath((Join-Path $rootPath $RelativePath))
    $relative = [IO.Path]::GetRelativePath($rootPath, $path)
    if ($relative -eq '..' -or $relative -match '^\.\.[\\/]' -or
        [IO.Path]::IsPathRooted($relative) -or
        [IO.Path]::GetExtension($path) -ne '.csproj' -or
        -not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw 'Manifest project must be an existing .csproj inside its session.'
    }
    $path
}

function Get-MafClawProjectInventory {
    param([Parameter(Mandatory)][string]$CodeRoot)
    $root = [IO.Path]::GetFullPath($CodeRoot)
    foreach ($file in Get-ChildItem -LiteralPath $root -Filter '*.csproj' -File -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git|\.local|\.azure)[\\/]' } |
        Sort-Object FullName) {
        $metadata = Invoke-MafClawProcess -FilePath 'dotnet' -WorkingDirectory $file.DirectoryName `
            -Arguments @('msbuild', $file.FullName, '-nologo',
                '-getProperty:TargetFramework,TargetFrameworks,OutputType,IsTestProject,ProjectAssetsFile',
                '-getItem:PackageReference,PackageVersion') -TimeoutSeconds 120
        if ($metadata.TimedOut -or $metadata.ExitCode -ne 0) {
            throw "MSBuild metadata failed for $([IO.Path]::GetRelativePath($root, $file.FullName)). Rerun privately for diagnostics."
        }
        $data = $metadata.StandardOutput | ConvertFrom-Json -AsHashtable
        $tfm = $data.Properties.TargetFrameworks
        if ([string]::IsNullOrWhiteSpace($tfm)) { $tfm = $data.Properties.TargetFramework }
        $central = @{}
        foreach ($item in @($data.Items.PackageVersion)) {
            if ($null -ne $item) { $central[$item.Identity] = $item.Version }
        }
        $packages = @(
            foreach ($item in @($data.Items.PackageReference)) {
                if ($null -eq $item) { continue }
                $version = $item['VersionOverride']
                if ([string]::IsNullOrWhiteSpace($version)) { $version = $item['Version'] }
                if ([string]::IsNullOrWhiteSpace($version)) { $version = $central[$item.Identity] }
                [pscustomobject]@{ Id = $item.Identity; Version = $version }
            }
        )
        [pscustomobject]@{
            Path = $file.FullName
            RelativePath = [IO.Path]::GetRelativePath($root, $file.FullName)
            Frameworks = @($tfm -split ';' | Where-Object { $_ })
            IsRunnable = $data.Properties.OutputType -in @('Exe', 'WinExe') -or
                $data.Properties.IsTestProject -eq 'true'
            IsTestProject = $data.Properties.IsTestProject -eq 'true'
            AssetsPath = $data.Properties.ProjectAssetsFile
            Packages = $packages
        }
    }
}

function Get-MafClawVerificationCases {
    param([Parameter(Mandatory)][string]$CodeRoot)
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in Get-ChildItem -LiteralPath $CodeRoot -Filter 'verification-manifest.json' -File -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git|\.local|\.azure)[\\/]' } |
        Sort-Object FullName) {
        $manifest = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -AsHashtable
        if ($manifest.schemaVersion -ne 1 -or $null -eq $manifest.cases) {
            throw 'Verification manifests require schemaVersion 1 and cases.'
        }
        foreach ($case in $manifest.cases) {
            $id = "$($file.Directory.Name):$($case.id)"
            if ([string]::IsNullOrWhiteSpace($case.id) -or -not $ids.Add($id)) {
                throw 'Verification case IDs must be nonempty and unique within a session.'
            }
            if ($case.mode -notin @('offline', 'live') -or $case.runner -notin @('run', 'test')) {
                throw "Invalid mode or runner for $id."
            }
            if ($case.timeoutSeconds -isnot [int] -and $case.timeoutSeconds -isnot [long]) {
                throw "A numeric timeoutSeconds is required for $id."
            }
            if ($case.timeoutSeconds -lt 1 -or $case.timeoutSeconds -gt 1800 -or
                ($case.expectedExitCode -isnot [int] -and $case.expectedExitCode -isnot [long])) {
                throw "Invalid timeout or expectedExitCode for $id."
            }
            foreach ($name in @('args', 'requiredOutput', 'forbiddenOutput', 'coversProjects')) {
                if ($null -ne $case[$name] -and
                    ($case[$name] -isnot [array] -or @($case[$name] | Where-Object { $_ -isnot [string] }).Count -gt 0)) {
                    throw "$name must be a string array for $id."
                }
            }
            if ($null -ne $case['stdin'] -and $case['stdin'] -isnot [string]) {
                throw "stdin must be a string for $id."
            }
            $minimumOutputLength = $case['minimumOutputLength'] ?? 0
            if (($minimumOutputLength -isnot [int] -and $minimumOutputLength -isnot [long]) -or
                $minimumOutputLength -lt 0 -or $minimumOutputLength -gt 1048576) {
                throw "Invalid minimumOutputLength for $id."
            }
            $environment = $case['environment'] ?? @{}
            if ($environment -isnot [Collections.IDictionary] -or @($environment.Keys |
                Where-Object { $_ -notmatch '^[A-Za-z_][A-Za-z0-9_]*$' -or $environment[$_] -isnot [string] }).Count -gt 0) {
                throw "environment must contain named string values for $id."
            }
            $project = Resolve-MafClawProjectPath -Root $file.DirectoryName -RelativePath $case.project
            $covered = @($project) + @(
                foreach ($path in @($case['coversProjects'])) {
                    if ($null -ne $path) {
                        Resolve-MafClawProjectPath -Root $file.DirectoryName -RelativePath $path
                    }
                }
            )
            [pscustomobject]@{
                Id = $id; Project = $project; WorkingDirectory = $file.DirectoryName
                Mode = $case.mode; Runner = $case.runner
                Arguments = @($case['args'] | Where-Object { $null -ne $_ })
                InputText = [string]$case['stdin']
                TimeoutSeconds = [int]$case.timeoutSeconds
                ExpectedExitCode = [int]$case.expectedExitCode
                RequiredOutput = @($case['requiredOutput'] | Where-Object { $null -ne $_ })
                ForbiddenOutput = @($case['forbiddenOutput'] | Where-Object { $null -ne $_ })
                MinimumOutputLength = [int]$minimumOutputLength
                Environment = $environment
                CoveredProjects = $covered
            }
        }
    }
}

function Test-MafClawCaseResult {
    param(
        [Parameter(Mandatory)]$Case,
        [Parameter(Mandatory)]$ProcessResult
    )
    $failures = [Collections.Generic.List[string]]::new()
    if ($ProcessResult.TimedOut) { $failures.Add('Execution timed out; owned process tree terminated.') }
    if ($ProcessResult.ExitCode -ne $Case.ExpectedExitCode) {
        $failures.Add("Expected exit $($Case.ExpectedExitCode); received $($ProcessResult.ExitCode).")
    }
    $output = $ProcessResult.StandardOutput + "`n" + $ProcessResult.StandardError
    if ($ProcessResult.StandardOutput.Trim().Length -lt $Case.MinimumOutputLength) {
        $failures.Add('Standard output is shorter than the declared minimum.')
    }
    foreach ($text in $Case.RequiredOutput) {
        if (-not $output.Contains($text, [StringComparison]::Ordinal)) {
            $failures.Add("Required output not found: $text")
        }
    }
    foreach ($text in $Case.ForbiddenOutput) {
        if ($output.Contains($text, [StringComparison]::Ordinal)) {
            $failures.Add("Forbidden output found: $text")
        }
    }
    [pscustomobject]@{
        Id = $Case.Id; Mode = $Case.Mode
        Status = if ($failures.Count -eq 0) { 'Pass' } else { 'Fail' }
        ExitCode = $ProcessResult.ExitCode
        DurationMilliseconds = $ProcessResult.DurationMilliseconds
        Failures = @($failures)
    }
}

Export-ModuleMember -Function Invoke-MafClawProcess, Resolve-MafClawProjectPath,
    Get-MafClawProjectInventory, Get-MafClawVerificationCases, Test-MafClawCaseResult
