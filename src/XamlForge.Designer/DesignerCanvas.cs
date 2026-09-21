using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using XamlForge.Core;

namespace XamlForge.Designer;
public sealed class DesignerCanvas : Grid
{
    readonly Grid page=new();readonly Canvas adorners=new();readonly Border selection=new(){BorderBrush=new SolidColorBrush(Color.FromRgb(0,122,204)),BorderThickness=new Thickness(1),IsHitTestVisible=false};
    readonly Thumb resize=new(){Width=9,Height=9,Background=Brushes.White,BorderBrush=Brushes.DodgerBlue,BorderThickness=new Thickness(1),Cursor=Cursors.SizeNWSE};
    readonly ScaleTransform zoom=new(1,1);readonly TextBlock title=new(){Margin=new Thickness(10,6,10,6),Foreground=Brushes.Black,FontSize=12};
    RenderedDesign? rendered;DesignDocument document;string? selected;Point start;bool moving;double width,height;Vector drag;
    public event Action<string>? SelectionChanged;public event Action<string,string,double,double>? ControlDropped;public event Action<string>? Error;
    public IReadOnlyList<Button> CaptionButtons {get;private set;}=[];
    public string? Selected=>selected;
    public RenderedDesign? Rendered=>rendered;
    public DesignerCanvas(DesignDocument doc)
    {
        document=doc;Background=new SolidColorBrush(Color.FromRgb(237,239,242));Focusable=true;AllowDrop=true;
        var frame=new Border {BorderBrush=new SolidColorBrush(Color.FromRgb(160,173,187)),BorderThickness=new Thickness(1),Margin=new Thickness(50),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,LayoutTransform=zoom};
        var outer=new DockPanel();
        var caption=new DockPanel{LastChildFill=true};
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(0,1,3,0)};
        var captions=new List<Button>();
        foreach(var item in new[]{("−","Minimize (design preview)"),("□","Maximize (design preview)"),("×","Close (design preview)")}){
            var button=new Button{Content=item.Item1,Width=28,Height=21,Padding=new Thickness(0),FontSize=14,
                Background=new SolidColorBrush(Color.FromRgb(224,233,241)),Foreground=Brushes.Black,
                BorderBrush=new SolidColorBrush(Color.FromRgb(95,113,132)),BorderThickness=new Thickness(1),Focusable=false,Tag=item.Item2,
                ToolTip=UiText.Get(item.Item2)};
            System.Windows.Automation.AutomationProperties.SetName(button,UiText.Get(item.Item2));
            // Caption controls represent the designed window, not the editor's own lifecycle.
            button.Click+=(s,e)=>Select(DesignDocument.NameOf(document.Xml.Root!));
            captions.Add(button);buttons.Children.Add(button);
        }
        CaptionButtons=captions;
        DockPanel.SetDock(buttons,Dock.Right);caption.Children.Add(buttons);caption.Children.Add(title);
        var bar=new Border{Background=new SolidColorBrush(Color.FromRgb(184,207,229)),Child=caption};
        DockPanel.SetDock(bar,Dock.Top);outer.Children.Add(bar);outer.Children.Add(page);frame.Child=outer;
        var scroll=new ScrollViewer{Content=frame,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Children.Add(scroll);
        adorners.Children.Add(selection);adorners.Children.Add(resize);
        page.PreviewMouseLeftButtonDown+=OnPointerDown;page.PreviewMouseMove+=OnPointerMove;page.PreviewMouseLeftButtonUp+=OnPointerUp;
        resize.DragStarted+=(s,e)=>{if(GetSelected() is {} fe){width=fe.ActualWidth;height=fe.ActualHeight;drag=new();}};
        resize.DragDelta+=(s,e)=>{drag+=new Vector(e.HorizontalChange,e.VerticalChange);selection.Width=Math.Max(8,width+drag.X);selection.Height=Math.Max(8,height+drag.Y);Canvas.SetLeft(resize,Canvas.GetLeft(selection)+selection.Width-4);Canvas.SetTop(resize,Canvas.GetTop(selection)+selection.Height-4);};
        resize.DragCompleted+=(s,e)=>{if(selected is null)return;Try(()=>document.SetProperties(selected,new Dictionary<string,string?>{{"Width",DesignDocument.Number(Math.Max(8,width+drag.X))},{"Height",DesignDocument.Number(Math.Max(8,height+drag.Y))}}));};
        PreviewDragOver+=(s,e)=>{e.Effects=e.Data.GetDataPresent("XamlForge.Control")?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};
        Drop+=(s,e)=>{if(e.Data.GetData("XamlForge.Control") is not string type)return;var pos=e.GetPosition(page);var parent=FindContainer(pos);var target=rendered?.Controls.GetValueOrDefault(parent);var local=target is null?pos:page.TranslatePoint(pos,target);ControlDropped?.Invoke(type,parent,Math.Max(0,local.X),Math.Max(0,local.Y));e.Handled=true;};
        SizeChanged+=(s,e)=>DrawSelection();Render();
    }
    public void SetZoom(double value){zoom.ScaleX=zoom.ScaleY=value;}
    public void ApplyChrome(bool dark){foreach(var button in CaptionButtons){button.ToolTip=UiText.Get((string)button.Tag);System.Windows.Automation.AutomationProperties.SetName(button,(string)button.ToolTip);}Background=new SolidColorBrush(dark?Color.FromRgb(30,30,30):Color.FromRgb(237,239,242));}
    public void Render()
    {
        var next=DesignRenderer.Render(document);rendered=next;
        if(selected is not null&&!document.Elements.Any(e=>DesignDocument.NameOf(e)==selected))selected=DesignDocument.NameOf(document.Xml.Root!);
        page.Children.Clear();page.Width=next.Surface.Width;page.Height=next.Surface.Height;page.Children.Add(next.Surface);page.Children.Add(adorners);
        title.Text=(string?)document.Xml.Root?.Attribute("Title")??"PowerShell Window";
        page.UpdateLayout();DrawSelection();Dispatcher.BeginInvoke(new Action(DrawSelection));
    }
    public void Select(string name){selected=name;DrawSelection();SelectionChanged?.Invoke(name);}
    FrameworkElement? GetSelected()=>selected is null?null:rendered?.Controls.GetValueOrDefault(selected);
    void DrawSelection()
    {
        var fe=GetSelected();if(fe is null||!fe.IsDescendantOf(page)){selection.Visibility=resize.Visibility=Visibility.Collapsed;return;}
        var p=fe.TranslatePoint(new Point(),page);selection.Visibility=resize.Visibility=Visibility.Visible;
        Canvas.SetLeft(selection,p.X);Canvas.SetTop(selection,p.Y);selection.Width=Math.Max(2,fe.ActualWidth);selection.Height=Math.Max(2,fe.ActualHeight);Canvas.SetLeft(resize,p.X+fe.ActualWidth-4);Canvas.SetTop(resize,p.Y+fe.ActualHeight-4);
    }
    string Hit(Point p)
    {
        if(rendered is null)return "MainWindow";
        var hit=VisualTreeHelper.HitTest(rendered.Surface,p)?.VisualHit;
        while(hit is not null){var match=rendered.Controls.FirstOrDefault(k=>ReferenceEquals(k.Value,hit));if(match.Key is not null)return match.Key;hit=VisualTreeHelper.GetParent(hit);}
        return DesignDocument.NameOf(document.Xml.Root!);
    }
    string FindContainer(Point p)
    {
        var name=Hit(p);var e=document.Find(name);
        while(e is not null){if(e.Name.LocalName is "Grid" or "Canvas" or "StackPanel" or "DockPanel" or "WrapPanel" or "Border" or "GroupBox" or "TabItem" or "Expander" or "ScrollViewer" or "Viewbox")return DesignDocument.NameOf(e);e=e.Parent;}
        return document.Elements.FirstOrDefault(e=>e.Name.LocalName is "Grid" or "Canvas" or "StackPanel") is {} panel?DesignDocument.NameOf(panel):DesignDocument.NameOf(document.Xml.Root!);
    }
    void OnPointerDown(object sender,MouseButtonEventArgs e)
    {
        if(e.OriginalSource is DependencyObject d){for(var n=d;n is not null;n=VisualTreeHelper.GetParent(n)){if(n==resize)return;if(n==page)break;}}
        Focus();start=e.GetPosition(page);Select(Hit(start));moving=selected!=DesignDocument.NameOf(document.Xml.Root!);page.CaptureMouse();e.Handled=true;
    }
    void OnPointerMove(object sender,MouseEventArgs e)
    {
        if(!moving||e.LeftButton!=MouseButtonState.Pressed)return;var delta=e.GetPosition(page)-start;selection.RenderTransform=new TranslateTransform(delta.X,delta.Y);e.Handled=true;
    }
    void OnPointerUp(object sender,MouseButtonEventArgs e)
    {
        if(!page.IsMouseCaptured)return;page.ReleaseMouseCapture();selection.RenderTransform=Transform.Identity;
        var delta=e.GetPosition(page)-start;if(moving&&delta.Length>2)MoveSelection(delta.X,delta.Y);moving=false;e.Handled=true;
    }
    public void MoveSelection(double dx,double dy)
    {
        if(selected is null)return;var xml=document.Find(selected);var fe=GetSelected();if(fe is null)return;
        if(xml.Parent?.Name.LocalName=="Canvas")Try(()=>document.SetProperties(selected,new Dictionary<string,string?>{{"Canvas.Left",DesignDocument.Number(Math.Max(0,Read(xml,"Canvas.Left")+dx))},{"Canvas.Top",DesignDocument.Number(Math.Max(0,Read(xml,"Canvas.Top")+dy))}}));
        else if(xml.Parent?.Name.LocalName=="Grid")Try(()=>document.SetProperties(selected,new Dictionary<string,string?>{{"Margin",$"{DesignDocument.Number(Math.Max(0,fe.Margin.Left+dx))},{DesignDocument.Number(Math.Max(0,fe.Margin.Top+dy))},{DesignDocument.Number(fe.Margin.Right)},{DesignDocument.Number(fe.Margin.Bottom)}"}}));
    }
    static double Read(System.Xml.Linq.XElement e,string attr)=>double.TryParse((string?)e.Attribute(attr),CultureInfo.InvariantCulture,out var n)?n:0;
    void Try(Action action){try{action();}catch(Exception ex){Error?.Invoke(ex.Message);}}
}

