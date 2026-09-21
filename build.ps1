[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')][string]$Configuration='Release',
    [switch]$BuildOnly
)
$ErrorActionPreference='Stop'
if(!$BuildOnly){& "$PSScriptRoot/publish.ps1" -Configuration $Configuration;return}
Push-Location $PSScriptRoot
try {
    dotnet build XamlForge.slnx -c $Configuration
    if($LASTEXITCODE -ne 0){throw 'XamlForge build failed.'}
} finally {Pop-Location}
