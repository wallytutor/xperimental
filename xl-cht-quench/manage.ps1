$script:PyEnv = [PSCustomObject]@{
    Version = "3.12";
    Path    = "$PSScriptRoot\venv"
}

$script:MeshFile  = "$PSScriptRoot\model\geometry.msh"
$script:ElmerMesh = "$PSScriptRoot\model\elmer"
$script:MergeTol  = 1.0e-05
$script:NumProc   = 4

function Invoke-ElmerGridConversion {
    if (-not (Test-Path -Path $script:MeshFile)) {
        & python geometry.py
    }

    $argumentList = @(
        "14",               # .msh (4) format
        "2",                # ElmerSolver format
        "$script:MeshFile", # Path to input file
        "-autoclean",
        "-merge", "$MergeTol",
        "-out", "$script:ElmerMesh"
    )

    Start-Process -NoNewWindow -Wait -FilePath "ElmerGrid.exe" `
        -ArgumentList $argumentList
}

function Invoke-ElmerGridPartition {
    # Parallel partitioning with METIS:
    $argumentList = @(
        "2",                 # ElmerSolver format
        "2",                 # ElmerSolver format
        "$script:ElmerMesh", # Path to input directory
        "-partdual",
        "-metiskway",
        "$script:NumProc"
    )

    Start-Process -NoNewWindow -Wait -FilePath "ElmerGrid.exe" `
        -ArgumentList $argumentList
}

function Invoke-ElmerSolverRun {
    param(
        [string]$SifFile
    )

    $pathPid = "ELMERSOLVER_PID"
    $workingDir = (Resolve-Path -Path ".").Path

    $job = Start-Job -ScriptBlock {
        param(
            [string]$workDir,
            [string]$sifFile,
            [int]$nproc
        )
        Set-Location $workDir

        $stdoutFile = "log.out-ElmerSolver-$nproc"
        $stderrFile = "log.err-ElmerSolver-$nproc"

        $p = Start-Process -FilePath "mpiexec" `
            -ArgumentList @("-n", "$nproc", "ElmerSolver_mpi.exe", "$sifFile") `
            -RedirectStandardOutput $stdoutFile `
            -RedirectStandardError  $stderrFile `
            -PassThru -NoNewWindow

        return $p.Id
    } -ArgumentList @($workingDir, $SifFile, $NumProc)

    Wait-Job -Id $job.Id -Timeout 10 | Out-Null
    $pidValue = Receive-Job -Id $job.Id
    Remove-Job -Id $job.Id -Force

    if ($null -ne $pidValue -and "$pidValue" -ne "") {
        "$pidValue" | Out-File -FilePath $pathPid -Encoding ascii -Force
        Write-Host "Stop simulation with the following command:`n"
        Write-Host "  Stop-Process -Id $pidValue -Force`n"
    }

    return $pidValue
}

function New-StopScript {
    param(
        [int]$PidValue
    )
    if ($null -ne $PidValue -and "$PidValue" -ne "") {
        "Stop-Process -Id $PidValue -Force" `
        | Out-File -FilePath "stop.ps1" -Encoding ascii -Force
    } else {
        Write-Host "Could not retrieve process PID."
    }
}

function Start-SimulationStep {
    param(
        [string]$SifFile
    )
    Push-Location "$PSScriptRoot\model"
    $pidValue = $null

    try {
        $pidValue = Invoke-ElmerSolverRun $SifFile
    } finally {
        Pop-Location
    }

    New-StopScript -PidValue $pidValue
    return $pidValue
}

if (-not (Test-Path $PyEnv.Path)) {
    Write-Host "Creating virtual environment at: $PyEnv.Path"
    & uv venv --seed --python $PyEnv.Version $PyEnv.Path
    & uv pip install -r "$PSScriptRoot\requirements.txt" --no-warn-script-location
}

if (-not $env:VIRTUAL_ENV) {
    Write-Host "Activating virtual environment..."
    . "$($PyEnv.Path)\Scripts\Activate.ps1"
}

if (-not (Test-Path $script:ElmerMesh)) {
    Invoke-ElmerGridConversion
}

if (-not (Test-Path "$script:ElmerMesh\partitioning.$script:NumProc")) {
    Invoke-ElmerGridPartition
}

# $pidWait = Start-SimulationStep -SifFile "0-init.sif"

# if ($pidWait -ne $null) {
#     Write-Host "Waiting for initialization to complete..."
#     Wait-Process -Id $pidWait -ErrorAction SilentlyContinue
# }

# Remove results from init here!

Start-SimulationStep -SifFile "1-case.sif"