using System.Windows;
using XamlForge.Core;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
namespace XamlForge.Designer;
public sealed record RenderedDesign(FrameworkElement Surface, IReadOnlyDictionary<string,FrameworkElement> Controls) { public Window? RootWindow {get;init;} }
public static class DesignRenderer
{
    public static RenderedDesign Render(DesignDocument document)
    {
        foreach(var assembly in document.Assemblies) System.Reflection.Assembly.LoadFrom(assembly);
        var window=(Window)XamlReader.Parse(document.Xaml);
        var controls=new Dictionary<string,FrameworkElement>();
        foreach(var e in document.Elements) {var name=DesignDocument.NameOf(e);if(name.Length>0 && window.FindName(name) is FrameworkElement control)controls[name]=control;}
        var content=window.Content as UIElement;window.Content=null;
        var surface=new Border { Tag="XamlForge.DesignSurface",Width=double.IsNaN(window.Width)?640:window.Width,Height=double.IsNaN(window.Height)?480:window.Height,Background=window.Background??Brushes.White,Resources=window.Resources,DataContext=window.DataContext,Child=content,ClipToBounds=true };
        surface.Resources[SystemColors.WindowBrushKey]=SystemColors.WindowBrush;surface.Resources[SystemColors.WindowTextBrushKey]=SystemColors.WindowTextBrush;surface.Resources[SystemColors.ControlBrushKey]=SystemColors.ControlBrush;surface.Resources[SystemColors.ControlTextBrushKey]=SystemColors.ControlTextBrush;
        System.Windows.Documents.TextElement.SetFontSize(surface,window.FontSize);
        System.Windows.Documents.TextElement.SetFontFamily(surface,window.FontFamily);
        System.Windows.Documents.TextElement.SetFontWeight(surface,window.FontWeight);
        System.Windows.Documents.TextElement.SetFontStyle(surface,window.FontStyle);
        System.Windows.Documents.TextElement.SetFontStretch(surface,window.FontStretch);
        System.Windows.Documents.TextElement.SetForeground(surface,window.Foreground);
        surface.FlowDirection=window.FlowDirection;surface.Language=window.Language;surface.UseLayoutRounding=window.UseLayoutRounding;surface.SnapsToDevicePixels=window.SnapsToDevicePixels;
        var rootName=DesignDocument.NameOf(document.Xml.Root!);if(rootName.Length>0)controls[rootName]=surface;
        return new(surface,controls) { RootWindow=window };
    }
}
