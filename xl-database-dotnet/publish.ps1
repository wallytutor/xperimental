param (
    [switch]$SingleFile,
    [switch]$SelfContained
)

$Project       = "xl_webapi/xl_webapi.csproj"
$Configuration = "Release"
$Framework     = "net10.0"
$Architecture  = "x64"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root

function Invoke-PublishProject {
    $rid = "win-$Architecture"
    dotnet publish $Project -c $Configuration -f $Framework -r $rid @args
}

function Invoke-PublishProjectSingleFile {
    param (
        [string]$Output
    )
    Invoke-PublishProject `
        --self-contained false `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $Output
}

function Invoke-PublishProjectFrameworkDependent {
    param (
        [string]$Output
    )
    Invoke-PublishProject `
        --self-contained false `
        -o $Output
}

if (-not $SingleFile -and -not $SelfContained) {
    Write-Host "> At least one of build mode must be specified." `
        -ForegroundColor Red
    Write-Host "> Use -SingleFile and/or -SelfContained switch." `
        -ForegroundColor Red
    exit 1
}

try {
    $distRoot   = Join-Path $root "dist"
    $singleRoot = Join-Path $distRoot "single"
    $normalRoot = Join-Path $distRoot "normal"
    $singleOut  = Join-Path $singleRoot $rid
    $normalOut  = Join-Path $normalRoot $rid

    if (-not (Test-Path $distRoot)) {
        New-Item -ItemType Directory -Path $distRoot | Out-Null
    }

    Write-Host "Publishing project ...: $Project"
    Write-Host "Configuration ........: $Configuration"
    Write-Host "Framework ............: $Framework"
    Write-Host "Architecture .........: $Architecture"
    Write-Host ""

    if ($SingleFile) {
        Write-Host "=== Single-file (framework-dependent) ==="
        Invoke-PublishProjectSingleFile -Output $singleOut
    }
    if ($SelfContained) {
        Write-Host "=== Self-contained (multi-file) ==="
        Invoke-PublishProjectFrameworkDependent -Output $normalOut
    }

    Write-Host ""
    Write-Host "Done. Outputs:"
    if ($SingleFile)    { Write-Host " - $singleRoot"}
    if ($SelfContained) { Write-Host " - $normalRoot" }
}
finally {
    Pop-Location
}
