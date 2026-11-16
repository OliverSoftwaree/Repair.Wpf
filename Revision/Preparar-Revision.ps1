[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $FuenteOliver,
    [Parameter(Mandatory)] [string] $FuenteTipoCsv,
    [Parameter(Mandatory)] [string] $Destino,
    [ValidateNotNullOrEmpty()] [string] $Servidor = '.'
)

$ErrorActionPreference = 'Stop'
$oliver = (Resolve-Path -LiteralPath $FuenteOliver).ProviderPath
$tipoCsv = (Resolve-Path -LiteralPath $FuenteTipoCsv).ProviderPath
$salida = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destino)
if (Test-Path -LiteralPath $salida) {
    throw 'Elige una carpeta de destino nueva para preparar el paquete.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) { throw 'No se encontró Visual Studio 2022.' }
$msbuild = & $vswhere -latest -products '*' -version '[17.0,18.0)' `
    -requires Microsoft.Component.MSBuild -find 'MSBuild\Current\Bin\MSBuild.exe' |
    Select-Object -First 1
if (-not $msbuild) { throw 'No se encontró MSBuild de Visual Studio 2022.' }

foreach ($solucion in @(
    (Join-Path $PSScriptRoot 'Repair.Seguimiento.sln'),
    (Join-Path $oliver 'Repair.Wpf.sln'),
    (Join-Path $tipoCsv 'Repair.Wpf.sln')
)) {
    & $msbuild $solucion /p:Configuration=Release '/p:Platform=Any CPU' /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) { throw "Falló la compilación de $solucion." }
}

$conexion = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$conexion['Data Source'] = $Servidor
$conexion['Initial Catalog'] = 'RepairSeguimientoDesarrollo_20261115'
$conexion['Integrated Security'] = $true
$conexion['Connect Timeout'] = 5
$conexion['Application Name'] = 'Repair.Revision'

function Copiar-Setup($origen, $nombre, $carpeta, [switch] $RepairOriginal) {
    Copy-Item -LiteralPath (Join-Path $origen $nombre) -Destination $carpeta
    [xml] $config = Get-Content -Raw -LiteralPath (Join-Path $origen "$nombre.config")
    $config.configuration.connectionStrings.SelectSingleNode("add[@name='RepairSql']").connectionString = $conexion.ConnectionString
    if ($RepairOriginal) {
        $config.configuration.appSettings.SelectSingleNode("add[@key='Modo']").value = 'Sql'
        Copy-Item -LiteralPath (Join-Path $origen 'Repair.Core.dll') -Destination $carpeta
    }
    $config.Save((Join-Path $carpeta "$nombre.config"))
}

New-Item -ItemType Directory -Path $salida | Out-Null
Copiar-Setup (Join-Path $PSScriptRoot 'Repair.Seguimiento\bin\Release') 'Repair.Revision.exe' $salida
Copiar-Setup (Join-Path $PSScriptRoot 'Repair.Seguimiento.Sql.Pruebas\bin\Release') 'Verificar-Repair-Seguimiento.exe' $salida

$carpetaOliver = (New-Item -ItemType Directory -Path (Join-Path $salida 'Oliver')).FullName
$carpetaTipoCsv = (New-Item -ItemType Directory -Path (Join-Path $salida 'TipoCsv')).FullName
Copiar-Setup (Join-Path $oliver 'Repair.Wpf\bin\Release') 'Repair.Wpf.exe' $carpetaOliver -RepairOriginal
Copiar-Setup (Join-Path $tipoCsv 'Repair.Wpf\bin\Release') 'Repair.Wpf.exe' $carpetaTipoCsv -RepairOriginal
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $salida
Write-Output "Paquete de revisión preparado en $salida"
