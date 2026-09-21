$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
$assets=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'src/XamlForge.App/Assets'))
$canvas=[Windows.Markup.XamlReader]::Parse([IO.File]::ReadAllText((Join-Path $assets 'XamlForgeIcon.xaml')))
$canvas.Measure([Windows.Size]::new(256,256));$canvas.Arrange([Windows.Rect]::new(0,0,256,256));$canvas.UpdateLayout()
$frames=@()
foreach($size in @(16,24,32,48,64,128,256)){
    $visual=[Windows.Media.DrawingVisual]::new();$drawing=$visual.RenderOpen()
    $drawing.DrawRectangle([Windows.Media.VisualBrush]::new($canvas),$null,[Windows.Rect]::new(0,0,$size,$size));$drawing.Close()
    $bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
    $encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap));$stream=[IO.MemoryStream]::new();$encoder.Save($stream)
    $frames+=@{Size=$size;Bytes=$stream.ToArray()};$stream.Dispose()
}
[IO.File]::WriteAllBytes((Join-Path $assets 'XamlForge.png'),$frames[-1].Bytes)
$file=[IO.File]::Create((Join-Path $assets 'XamlForge.ico'));$writer=[IO.BinaryWriter]::new($file)
try{
    $writer.Write([UInt16]0);$writer.Write([UInt16]1);$writer.Write([UInt16]$frames.Count);$offset=6+16*$frames.Count
    foreach($frame in $frames){$dimension=if($frame.Size -eq 256){0}else{$frame.Size};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([UInt16]1);$writer.Write([UInt16]32);$writer.Write([UInt32]$frame.Bytes.Length);$writer.Write([UInt32]$offset);$offset+=$frame.Bytes.Length}
    foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
}finally{$writer.Dispose()}
Write-Output 'Created XamlForge icon: 16, 24, 32, 48, 64, 128 and 256 px.'
