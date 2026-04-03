$script:PyEnv = [PSCustomObject]@{
    Version = "3.12"
    Path    = Join-Path $PSScriptRoot "venv"
}

function Start-Workflow {
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
}

Start-Workflow