[CmdletBinding()]
param([switch]$Published,[switch]$NoBuild)
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
if($Published){
    $exe=Join-Path $projectRoot 'artifacts/app/XamlForge.exe'
    if(!(Test-Path -LiteralPath $exe)){& "$PSScriptRoot/publish.ps1"}
}else{
    if(!$NoBuild){& "$PSScriptRoot/build.ps1" -BuildOnly -Configuration Debug}
    $exe=Join-Path $projectRoot 'src/XamlForge.App/bin/Debug/net10.0-windows/win-x64/XamlForge.exe'
}
if(!(Test-Path -LiteralPath $exe)){throw "Build missing: $exe"}
Start-Process -FilePath $exe -WindowStyle Normal

