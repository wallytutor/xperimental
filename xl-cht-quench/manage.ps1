param (
    [switch]$RebuildMesh,
    [switch]$Reinitialize,
    [switch]$Simulate,
    [string]$NumDimensions = "2d"
)

$script:PyEnv = [PSCustomObject]@{
    Version = "3.12";
    Path    = "$PSScriptRoot\venv"
}

$script:ModelDir  = "$PSScriptRoot\model-$NumDimensions"
$script:GmshFile  = "$script:ModelDir\geometry.py"
$script:MeshFile  = "$script:ModelDir\geometry.msh"
$script:ElmerMesh = "$script:ModelDir\elmer"
$script:MergeTol  = 1.0e-05
$script:NumProc   = 4

function Push-PopLocation {
    param(
        [string]$Path
    )
    Push-Location $Path
    try {
        & $args[0]
    } finally {
        Pop-Location
    }
}

function Invoke-ElmerGridConversion {
    if (-not (Test-Path -Path $script:MeshFile)) {
        Push-PopLocation $script:ModelDir {
            & python $script:GmshFile
        }
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
    Push-Location "$script:ModelDir"

    Remove-Item -Path "results" -Recurse -Force -ErrorAction SilentlyContinue

    $pidValue = $null

    try {
        $pidValue = Invoke-ElmerSolverRun $SifFile
    } finally {
        Pop-Location
    }

    New-StopScript -PidValue $pidValue
    return $pidValue
}

function Start-Workflow {
    if ($RebuildMesh) {
        Write-Host "Rebuilding mesh..."
        Remove-Item -Path $script:MeshFile, $script:ElmerMesh `
            -Recurse -Force -ErrorAction SilentlyContinue
    }

    if ($Reinitialize) {
        Write-Host "Reinitializing simulation..."
        Remove-Item -Path "$script:ModelDir\init.result*" `
            -Recurse -Force -ErrorAction SilentlyContinue
    }

    $activate = Join-Path $PyEnv.Path "Scripts\\Activate.ps1"

    if (-not (Test-Path $activate)) {
        Write-Host "Creating virtual environment at: $($PyEnv.Path)"
        & uv venv --seed --python $PyEnv.Version $PyEnv.Path
        if ($LASTEXITCODE -ne 0) { throw "uv venv failed." }

        . $activate

        Write-Host "Installing dependencies..."
        & uv pip install -r (Join-Path $PSScriptRoot "requirements.txt")
        if ($LASTEXITCODE -ne 0) { throw "uv pip install failed." }
    }

    if (-not $env:VIRTUAL_ENV) {
        Write-Host "Activating virtual environment..."
        . $activate
    }

    $pathMesh = $script:ElmerMesh
    $pathPart = "$script:ElmerMesh\partitioning.$script:NumProc"

    if (-not (Test-Path $pathMesh)) {
        Invoke-ElmerGridConversion
    }

    if (-not (Test-Path $pathPart)) {
        Invoke-ElmerGridPartition
    }

    if (-not (Test-Path $pathPart)) {
        Write-Host "Cannot continue without partitioned mesh." `
            -ForegroundColor Red
        Write-Host "Please check the ElmerGrid output for errors." `
            -ForegroundColor Red
        exit 1
    }

    if (-not (Test-Path "$script:ModelDir\init.result*")) {
        $pidWait = Start-SimulationStep -SifFile "0-init.sif"

        if ($pidWait -ne $null) {
            Write-Host "Waiting for initialization to complete..."
            Wait-Process -Id $pidWait -ErrorAction SilentlyContinue
        }

        $Simulate = $true
    }

    if ($Simulate) {
        Write-Host "Starting simulation..."
        Start-SimulationStep -SifFile "1-case.sif"
        exit 0
    }
}

Start-Workflow
