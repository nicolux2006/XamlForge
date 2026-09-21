using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using XamlForge.Core;

namespace XamlForge.Designer;
public static class UiAppearance
{
    public static AppSettings Settings {get;private set;}=new();
    public static bool Dark {get;private set;}
    static readonly DependencyProperty OriginalText=DependencyProperty.RegisterAttached("OriginalText",typeof(string),typeof(UiAppearance));
    static readonly DependencyProperty OriginalHeader=DependencyProperty.RegisterAttached("OriginalHeader",typeof(string),typeof(UiAppearance));
    static readonly DependencyProperty OriginalTip=DependencyProperty.RegisterAttached("OriginalTip",typeof(string),typeof(UiAppearance));
    static readonly Dictionary<(bool,bool),ControlTemplate> templates=[];
    public static void Configure(AppSettings settings,bool dark){Settings=settings;Dark=dark;UiText.Language=settings.ResolveLanguage();}
    public static Brush Surface=>Brush(Dark?"#252526":"#FFFFFF");
    public static Brush Text=>Brush(Dark?"#E5E5E5":"#202020");
    static SolidColorBrush Brush(string color)=>(SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
    public static void Apply(DependencyObject root)
    {
        // The authored WPF page is intentionally a separate appearance boundary.
        if(root is FrameworkElement fe&&fe.Tag is "XamlForge.DesignSurface")return;
        if(root is FrameworkElement layout){layout.UseLayoutRounding=true;layout.SnapsToDevicePixels=true;}
        if(root is CodeEditor editor){editor.ApplyAppearance(Settings,Dark);return;}
        if(root is TextBlock label){Localize(label,OriginalText,()=>label.Text,v=>label.Text=v);label.Foreground=Text;}
        if(root is Control control){control.Foreground=Text;control.Background=Surface;control.BorderBrush=Brush(Dark?"#505054":"#C8CDD4");}
        if(root is Panel panel)panel.Background=Surface;
        if(root is Border border&&border.Parent is Workspace){border.Background=Brush("#007ACC");}
        if(root is GridSplitter splitter)splitter.Background=Brush(Dark?"#3F3F46":"#DADDE2");
        if(root is ContentControl content&&content.Content is string){Localize(content,OriginalText,()=>content.Content as string??"",v=>content.Content=v);}
        if(root is HeaderedContentControl hc&&hc.Header is string)Localize(hc,OriginalHeader,()=>hc.Header as string??"",v=>hc.Header=v);
        if(root is HeaderedItemsControl hi&&hi.Header is string)Localize(hi,OriginalHeader,()=>hi.Header as string??"",v=>hi.Header=v);
        if(root is FrameworkElement element&&element.ToolTip is string)Localize(element,OriginalTip,()=>element.ToolTip as string??"",v=>element.ToolTip=v);
        if(root is Control c)
        {
            foreach(var key in new[]{SystemColors.WindowBrushKey,SystemColors.ControlBrushKey})c.Resources[key]=Surface;
            foreach(var key in new[]{SystemColors.WindowTextBrushKey,SystemColors.ControlTextBrushKey})c.Resources[key]=Text;
            c.Resources[SystemColors.HighlightBrushKey]=Brush(Dark?"#094771":"#CCE8FF");c.Resources[SystemColors.HighlightTextBrushKey]=Text;
            c.Resources["XamlForge.Hover"]=Brush(Dark?"#3E3E42":"#E5EDF5");
            c.Resources["XamlForge.Selected"]=Brush(Dark?"#094771":"#D9ECFA");
            c.Resources["XamlForge.SelectionBorder"]=Brush(Dark?"#75BFFF":"#007ACC");
            if(c is MenuItem menu){menu.BorderThickness=new Thickness(0);menu.Template=EditorControlTemplates.MenuItem;}
            if(c is TreeViewItem)c.Template=EditorControlTemplates.TreeItem;
            if(c is ListBox list){var style=new Style(typeof(ListBoxItem));style.Setters.Add(new Setter(Control.TemplateProperty,EditorControlTemplates.ListItem));style.Setters.Add(new Setter(Control.ForegroundProperty,Text));style.Setters.Add(new Setter(Control.BackgroundProperty,Surface));style.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(5,4,5,4)));list.ItemContainerStyle=style;}
            if(c is ComboBox combo){
                combo.Template=EditorControlTemplates.ComboBox;
                var items=new Style(typeof(ComboBoxItem));items.Setters.Add(new Setter(Control.TemplateProperty,EditorControlTemplates.ComboItem));items.Setters.Add(new Setter(Control.ForegroundProperty,Text));items.Setters.Add(new Setter(Control.BackgroundProperty,Surface));items.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(7,5,7,5)));combo.ItemContainerStyle=items;
            }
            if(c is Button or TabItem)c.Template=Template(c is TabItem);
        }
        if(root is DesignerCanvas canvas){canvas.ApplyChrome(Dark);return;}
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>().ToArray())Apply(child);
        if(root is Border footer&&footer.Parent is Workspace&&footer.Child is TextBlock status)status.Foreground=System.Windows.Media.Brushes.White;
    }
    static void Localize(DependencyObject target,DependencyProperty key,Func<string> get,Action<string> set)
    {
        var current=get();var original=target.GetValue(key) as string;
        // Dynamic titles/status text may have changed independently since the last language switch.
        if(original is null||current!=original&&current!=Translate(original,"de")&&current!=Translate(original,"en")){original=current;target.SetValue(key,original);}
        set(UiText.Message(original));
    }
    static string Translate(string value,string language){var previous=UiText.Language;UiText.Language=language;try{return UiText.Message(value);}finally{UiText.Language=previous;}}
    static ControlTemplate Template(bool tab)
    {
        if(templates.TryGetValue((Dark,tab),out var cached))return cached;
        var bg=Dark?"#2D2D30":"#F4F5F7";var hover=Dark?"#3E3E42":"#E5EDF5";var selected=Dark?"#094771":"#D9ECFA";var fg=Dark?"#EEEEEE":"#202020";
        var trigger=tab?$"<Trigger Property='IsSelected' Value='True'><Setter TargetName='Frame' Property='Background' Value='{selected}'/><Setter TargetName='Frame' Property='BorderBrush' Value='#007ACC'/></Trigger>":"";
        var result=(ControlTemplate)XamlReader.Parse($"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{(tab?"TabItem":"Button")}'><Border x:Name='Frame' Background='{bg}' BorderBrush='{(Dark?"#505054":"#CBD0D7")}' BorderThickness='1' Padding='{{TemplateBinding Padding}}'><ContentPresenter ContentSource='{(tab?"Header":"Content")}' HorizontalAlignment='Center' VerticalAlignment='Center' RecognizesAccessKey='True' TextElement.Foreground='{fg}'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Frame' Property='Background' Value='{hover}'/></Trigger>{trigger}<Trigger Property='IsEnabled' Value='False'><Setter TargetName='Frame' Property='Opacity' Value='0.45'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");templates[(Dark,tab)]=result;return result;
    }
}
