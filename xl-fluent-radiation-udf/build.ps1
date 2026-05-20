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

Write-Host "Compiling shared library (wsgg_radlib.dll)..." `
    -ForegroundColor Green

# Compile wsgg_radlib_bordbar_2020.c into wsgg_radlib.dll
gcc -shared -o wsgg_radlib.dll `
    -fPIC -O2 -Wall -Wextra -ansi -pedantic `
    'src/wsgg_radlib_bordbar_2020.c'

# ---------------------------------------------------------------------

Write-Host "Compiling simple application (wsgg_app.exe)..." `
    -ForegroundColor Green

# Compile main.c and link it dynamically
gcc -O2 -Wall -Wextra -ansi -pedantic `
    -o wsgg_app.exe 'src/main.c' -L. -lwsgg_radlib

# ---------------------------------------------------------------------

Write-Host "Build Succeeded!" -ForegroundColor Green
Write-Host "To run the application: .\wsgg_app.exe" `
    -ForegroundColor Yellow

# ---------------------------------------------------------------------