param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/output-paths.ps1"
Push-Location $PSScriptRoot
$stage=$null
try {
    $artifactRoot=[IO.Path]::GetFullPath((Join-Path (Get-Location) 'artifacts'))
    $destination=Assert-OutputPath (Join-Path $artifactRoot 'app')
    $running=Get-Process XamlForge -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $destination 'XamlForge.exe')}
    if($running){throw 'Close the published XamlForge app before replacing it.'}
    $stage=Assert-OutputPath (Join-Path $artifactRoot ('publish-'+[Guid]::NewGuid().ToString('N')))
    dotnet publish src/XamlForge.App/XamlForge.App.csproj -c $Configuration -r win-x64 --self-contained true -o $stage
    if($LASTEXITCODE -ne 0){throw 'Publish failed; previous app is unchanged.'}
    Copy-Item -LiteralPath README.md -Destination (Join-Path $stage 'README.md')
    Copy-Item -LiteralPath THIRD-PARTY-NOTICES.md -Destination (Join-Path $stage 'THIRD-PARTY-NOTICES.md')
    Copy-Item -LiteralPath third-party-licenses -Destination $stage -Recurse
    Copy-Item -LiteralPath examples -Destination $stage -Recurse
    $backup=$null
    if(Test-Path -LiteralPath $destination){
        $preferences=Join-Path $destination 'settings.json'
        if(Test-Path -LiteralPath $preferences){Copy-Item -LiteralPath $preferences -Destination (Join-Path $stage 'settings.json')}
        $backup=Assert-OutputPath (Join-Path $artifactRoot ('previous-app-'+[Guid]::NewGuid().ToString('N')))
        Move-Item -LiteralPath $destination -Destination $backup
    }
    try {Move-Item -LiteralPath $stage -Destination $destination}
    catch {
        if($backup -and !(Test-Path -LiteralPath $destination)){Move-Item -LiteralPath $backup -Destination $destination}
        throw
    }
    if($backup){Remove-OutputDirectory $backup}
    Write-Output "Application: $(Join-Path $destination 'XamlForge.exe')"
} finally {
    if($stage -and (Test-Path -LiteralPath $stage)){Remove-OutputDirectory $stage}
    Pop-Location
}
