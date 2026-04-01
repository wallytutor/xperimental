param (
    [switch]$Run,
    [switch]$Clean,
    [int]$NumberProc = 4
)

function Invoke-ForceClean($what) {
    Write-Host "Cleaning $what..." -ForegroundColor Yellow
    Remove-Item -ErrorAction SilentlyContinue -Force -Recurse -Path $what
}

function Invoke-RunManager {
    param(
        [string]$Cfg,
        [string]$WorkingDir
    )

    $resolvedWorkingDir = (Resolve-Path -Path $WorkingDir).Path
    $stdoutPath = Join-Path $resolvedWorkingDir "log.solve-$NumberProc"
    $stderrPath = Join-Path $resolvedWorkingDir "log.error-$NumberProc"

    $job = Start-Job -ScriptBlock {
        param(
            [int]$nproc,
            [string]$cfg,
            [string]$workingDir,
            [string]$stdoutFile,
            [string]$stderrFile
        )
        Push-Location $workingDir
        try {
            $p = Start-Process -FilePath "mpiexec.exe" `
                -ArgumentList @("-n", "$nproc", "SU2_CFD.exe", $cfg) `
                -WorkingDirectory $workingDir `
                -RedirectStandardOutput $stdoutFile `
                -RedirectStandardError  $stderrFile `
                -PassThru -NoNewWindow -ErrorAction Stop

            $p.Id
        } finally {
            Pop-Location
        }
    } -ArgumentList @($NumberProc, $Cfg, $resolvedWorkingDir, $stdoutPath, $stderrPath)

    Wait-Job -Id $job.Id -Timeout 10 | Out-Null
    $pidValue = Receive-Job -Id $job.Id
    Remove-Job -Id $job.Id -Force

    if ($null -ne $pidValue -and "$pidValue" -ne "") {
        "Stop-Process -Id $pidValue -Force" `
        | Out-File -FilePath "stop.ps1" -Encoding ascii -Force

        Write-Host "Started background process PID: $pidValue"
        Write-Host "STDOUT log: $stdoutPath"
        Write-Host "STDERR log: $stderrPath"
        Write-Host "Stop simulation with the following command:`n"
        Write-Host "  Stop-Process -Id $pidValue -Force`n"
    } else {
        Write-Host "Could not retrieve PID..." -ForegroundColor Red
    }
}

Push-Location "$PSScriptRoot/model-su2"

try {
    if ($Clean) {
        Invoke-ForceClean "log.*"
        Invoke-ForceClean "*.log"
        Invoke-ForceClean "*.csv"
        Invoke-ForceClean "volume_*"
        Invoke-ForceClean "solution_*"
        Invoke-ForceClean "restart*"
    }

    if ($Run) {
        New-Item -Path "restart" -ItemType Directory `
            -ErrorAction SilentlyContinue | Out-Null

        Invoke-RunManager -Cfg "master.cfg" -WorkingDir "."
    }
} finally {
    Pop-Location
}