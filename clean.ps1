[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference='Stop'
. "$PSScriptRoot/output-paths.ps1"
$projectRoot=[IO.Path]::GetFullPath($PSScriptRoot)
$targets=@()
foreach($group in @('src')){
    foreach($project in Get-ChildItem -LiteralPath (Join-Path $projectRoot $group) -Directory){
        foreach($name in @('bin','obj')){
            $path=Join-Path $project.FullName $name
            if(Test-Path -LiteralPath $path){$targets+=$path}
        }
    }
}
$artifacts=Join-Path $projectRoot 'artifacts'
if(Test-Path -LiteralPath $artifacts){
    foreach($directory in Get-ChildItem -LiteralPath $artifacts -Directory){
        if($directory.Name -match '^(host-[0-9a-f]{32}|previous-app-[0-9a-f]{32}|publish-[0-9a-f]{32}|test-host|tests)$'){$targets+=$directory.FullName}
    }
}
foreach($path in $targets){
    $absolute=Assert-OutputPath $path
    $running=Get-Process XamlForge -ErrorAction SilentlyContinue | Where-Object {$_.Path -and $_.Path.StartsWith($absolute+'\',[StringComparison]::OrdinalIgnoreCase)}
    if($running){throw "Close the development app before cleaning: $absolute"}
    if($PSCmdlet.ShouldProcess($absolute,'Remove generated files (preserve settings.json)')){
        $settings=@{}
        if((Split-Path $absolute -Leaf) -eq 'bin'){
            foreach($file in Get-ChildItem -LiteralPath $absolute -Filter settings.json -Recurse -File){$settings[$file.FullName]=[IO.File]::ReadAllBytes($file.FullName)}
        }
        Remove-OutputDirectory $absolute
        foreach($file in $settings.Keys){[IO.Directory]::CreateDirectory((Split-Path $file)) | Out-Null;[IO.File]::WriteAllBytes($file,$settings[$file])}
        Write-Output "Cleaned $absolute"
    }
}
Write-Output 'Source, examples and artifacts/app are preserved.'
