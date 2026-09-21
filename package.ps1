[CmdletBinding()]
param([switch]$SkipPublish)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath($PSScriptRoot)
Push-Location $projectRoot
try {
    if(!$SkipPublish){& "$PSScriptRoot/publish.ps1"}
    $release=Join-Path $projectRoot 'artifacts/app'
    if(!(Test-Path -LiteralPath (Join-Path $release 'XamlForge.exe'))){throw 'Release missing. Run publish.ps1 first.'}
    [xml]$properties=Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw
    $version=[string]$properties.Project.PropertyGroup.Version
    if($version -notmatch '^\d+\.\d+\.\d+$'){throw 'Unexpected project version.'}
    $output=Join-Path $projectRoot 'dist'
    if((Test-Path -LiteralPath $output) -and ((Get-Item -LiteralPath $output -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Refusing linked output directory.'}
    New-Item -ItemType Directory -Path $output -Force | Out-Null

    function Write-Archive([string]$Destination,[string]$BasePath,[IO.FileInfo[]]$Files) {
        $temporary=$Destination+'.tmp'
        if((Test-Path -LiteralPath $temporary) -and ((Get-Item -LiteralPath $temporary -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Refusing linked temporary archive.'}
        try {
            $stream=[IO.File]::Open($temporary,[IO.FileMode]::Create,[IO.FileAccess]::Write,[IO.FileShare]::None)
            $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create)
            try {
                foreach($file in $Files | Sort-Object FullName){
                    if($file.Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Refusing linked file: $($file.FullName)"}
                    $relative=[IO.Path]::GetRelativePath($BasePath,$file.FullName).Replace('\','/')
                    if($relative.StartsWith('../')){throw 'Archive input outside source folder.'}
                    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,'XamlForge/'+$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
                }
            } finally {$archive.Dispose();$stream.Dispose()}
            Move-Item -LiteralPath $temporary -Destination $Destination -Force
        } finally {if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}}
    }

    $sourceFiles=@()
    foreach($name in @('.gitignore','.gitattributes','global.json','Directory.Build.props','NuGet.Config','XamlForge.slnx','README.md','THIRD-PARTY-NOTICES.md','build.ps1','publish.ps1','run.ps1','clean.ps1','package.ps1','output-paths.ps1','build-icon.ps1')){
        $sourceFiles+=Get-Item -LiteralPath (Join-Path $projectRoot $name) -Force
    }
    if(Test-Path -LiteralPath (Join-Path $projectRoot 'LICENSE')){$sourceFiles+=Get-Item -LiteralPath (Join-Path $projectRoot 'LICENSE')}
    foreach($name in @('src','examples','third-party-licenses')){
        $folder=Join-Path $projectRoot $name
        if((Get-Item -LiteralPath $folder -Force).Attributes -band [IO.FileAttributes]::ReparsePoint){throw "Refusing linked folder: $folder"}
        if(Get-ChildItem -LiteralPath $folder -Recurse -Force -Attributes ReparsePoint){throw "Linked source content: $folder"}
        $sourceFiles+=Get-ChildItem -LiteralPath $folder -Recurse -File -Force | Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Name -notmatch '^(settings\.json(?:\.tmp)?)$' -and $_.Extension -notin @('.log','.user','.suo')
        }
    }
    if(Get-ChildItem -LiteralPath $release -Recurse -Force -Attributes ReparsePoint){throw 'Linked release content.'}
    $releaseFiles=@(Get-ChildItem -LiteralPath $release -Recurse -File -Force | Where-Object {
        $_.Extension -notin @('.pdb','.log') -and $_.Name -notmatch '^settings\.json(?:\.tmp)?$'
    })
    $sourceZip=Join-Path $output "XamlForge-$version-source.zip"
    $releaseZip=Join-Path $output "XamlForge-$version-win-x64.zip"
    Write-Archive $sourceZip $projectRoot $sourceFiles
    Write-Archive $releaseZip $release $releaseFiles
    @($sourceZip,$releaseZip) | ForEach-Object {
        $hash=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $([IO.Path]::GetFileName($_))"
    } | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding utf8
    Write-Output "Repository ZIP: $sourceZip"
    Write-Output "Release ZIP: $releaseZip"
    Write-Output "Checksums: $(Join-Path $output 'SHA256SUMS.txt')"
} finally {Pop-Location}
