# build.ps1
#
# Powershell script for compiling the WSGG shared library
# using Mingw64 GCC on Windows

# ---------------------------------------------------------------------

$ErrorActionPreference = "Stop"

# Check if gcc is available
if (!(Get-Command gcc -ErrorAction SilentlyContinue)) {
    Write-Error "GCC compiler not found in Path."
    Write-Error "Please make sure Mingw64 is installed correctly."
}

# ---------------------------------------------------------------------

Write-Host "Compiling shared library (wsgglib.dll)..." `
    -ForegroundColor Green

gcc -shared -o wsgglib.dll `
    -fPIC -O2 -Wall -Wextra -ansi -pedantic `
    '../src/wsgglib.c'

# ---------------------------------------------------------------------

Write-Host "Compiling simple application (wsggapp.exe)..." `
    -ForegroundColor Green

gcc -O2 -Wall -Wextra -ansi -pedantic -I'../src' `
    -o wsggapp.exe 'wsggapp.c' -L. -lwsgglib

# ---------------------------------------------------------------------

Write-Host "Build Succeeded!" -ForegroundColor Green
Write-Host "To run the application: .\wsggapp.exe" `
    -ForegroundColor Yellow

# ---------------------------------------------------------------------