$PyEnv = [PSCustomObject]@{ Version = "3.12"; Path = "$PSScriptRoot\venv" }
$PyExe = Join-Path -Path $PyEnv.Path -ChildPath "Scripts\python.exe"

if (-not (Test-Path -Path $PyEnv.Path)) {
    Write-Host "Creating virtual environment at: $PyEnv.Path"
   & uv venv --seed --python $PyEnv.Version $PyEnv.Path
   & uv pip install -r "$PSScriptRoot\requirements.txt" --no-warn-script-location
}

if (-not $env:VIRTUAL_ENV) {
    Write-Host "Activating virtual environment..."
    . "$($PyEnv.Path)\Scripts\Activate.ps1"
}