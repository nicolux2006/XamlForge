# Shared guard for generated output only. Never accepts the project root or source folders.
function Assert-OutputPath([string]$Path) {
    $projectRoot=[IO.Path]::GetFullPath($PSScriptRoot)
    $absolute=[IO.Path]::GetFullPath($Path)
    $prefix=$projectRoot+[IO.Path]::DirectorySeparatorChar
    if(!$absolute.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw "Output outside project: $absolute"}
    $relative=$absolute.Substring($prefix.Length).Replace('\','/')
    if($relative -notmatch '^(artifacts/[^/]+|(?:src|tests)/[^/]+/(?:bin|obj))$'){throw "Not a generated output directory: $absolute"}
    for($ancestor=$absolute;$ancestor.Length -ge $projectRoot.Length;$ancestor=Split-Path $ancestor){
        if((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw "Refusing linked output path: $ancestor"}
    }
    return $absolute
}

function Remove-OutputDirectory([string]$Path) {
    $absolute=Assert-OutputPath $Path
    if(!(Test-Path -LiteralPath $absolute)){return}
    if(Get-ChildItem -LiteralPath $absolute -Recurse -Force -Attributes ReparsePoint){throw "Refusing output containing directory links: $absolute"}
    Remove-Item -LiteralPath $absolute -Recurse -Force
}
