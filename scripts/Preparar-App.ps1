$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Push-Location "$root\ui"
try {
    npm.cmd ci --prefer-offline
    if($LASTEXITCODE){throw 'npm ci falló'}
    npm.cmd run build
    if($LASTEXITCODE){throw 'Falló la compilación Vue'}
    npm.cmd test
    if($LASTEXITCODE){throw 'Fallaron las pruebas'}
} finally { Pop-Location }
Push-Location $root
try {
    dotnet publish VrcShelf.Desktop.csproj -c Release -r win-x64 --self-contained true -o dist-next -p:RuntimeFrameworkVersion=8.0.26
    if($LASTEXITCODE){throw 'Falló la publicación'}
    if(!(Test-Path -LiteralPath 'dist-next\web\index.html')){throw 'Falta la interfaz publicada'}
    if(Get-Process -Name 'VRC Shelf' -ErrorAction SilentlyContinue | Where-Object Path -eq (Join-Path $root 'dist\VRC Shelf.exe')){throw 'Cierra la aplicación antes de instalar la actualización'}
    if(Test-Path -LiteralPath dist){Copy-Item -Path 'dist-next\*' -Destination dist -Recurse -Force}
    else { Rename-Item -LiteralPath dist-next -NewName dist }
} finally { Pop-Location }

