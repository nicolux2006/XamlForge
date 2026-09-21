using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using XamlForge.Core;

namespace XamlForge.Designer;
public sealed class DesignView : Grid
{
    public DesignDocument Document {get;}
    public DesignerCanvas Canvas {get;}
    public CodeEditor Source {get;}=new();
    public CodeEditor HandlerEditor {get;}=new(true);
    public InspectorPane Inspector {get;}=new();
    public TreeView Hierarchy {get;}=new(){BorderThickness=new Thickness(0)};
    public string? FilePath {get;set;}
    public string Title {get;set;}="Untitled.xaml";
    public bool Dirty=>Document.Serialize()!=saved||sourceDirty||handlerDirty;
    public event Action? Changed;public event Action<string>? Status;
    readonly TabControl modes=new(){BorderThickness=new Thickness(0)};readonly Grid split=new();readonly ContentControl canvasHost=new(),sourceHost=new();
    readonly ComboBox handlerList=new(){MinWidth=200,Margin=new Thickness(6)};readonly TextBlock handlerTitle=new(){VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6)};
    readonly DispatcherTimer parseTimer=new(){Interval=TimeSpan.FromMilliseconds(650)};
    string saved;bool syncing,sourceDirty,handlerDirty;string? eventControl,eventName,copyName;
    public DesignView(DesignDocument doc)
    {
        Document=doc;saved=doc.Serialize();Canvas=new(doc);Background=Brushes.White;
        ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(5)});ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(360),MinWidth=300});
        Children.Add(modes);var divider=new GridSplitter{Width=5,HorizontalAlignment=HorizontalAlignment.Stretch,Background=new SolidColorBrush(Color.FromRgb(218,222,227))};SetColumn(divider,1);Children.Add(divider);SetColumn(Inspector,2);Children.Add(Inspector);
        var designPanel=new DockPanel();var bottom=new StackPanel{Orientation=Orientation.Horizontal,Background=new SolidColorBrush(Color.FromRgb(248,248,248))};
        bottom.Children.Add(new TextBlock{Text="Zoom",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8)});var zoom=new Slider{Minimum=.25,Maximum=2,Value=1,Width=130,Margin=new Thickness(5)};zoom.ValueChanged+=(s,e)=>Canvas.SetZoom(e.NewValue);bottom.Children.Add(zoom);bottom.Children.Add(new TextBlock{Text="Drag controls onto the page · Arrow keys move · Delete removes",Foreground=Brushes.Gray,Margin=new Thickness(12,6,0,6)});DockPanel.SetDock(bottom,Dock.Bottom);designPanel.Children.Add(bottom);designPanel.Children.Add(canvasHost);
        modes.Items.Add(new TabItem{Header="Design",Content=designPanel});
        var sourcePanel=new DockPanel();var sourceBar=new StackPanel{Orientation=Orientation.Horizontal};sourceBar.Children.Add(ActionButton("Apply XAML  Ctrl+Enter",()=>ApplySource()));sourceBar.Children.Add(new TextBlock{Text="Changes update the design after a short pause.",VerticalAlignment=VerticalAlignment.Center,Foreground=Brushes.Gray});DockPanel.SetDock(sourceBar,Dock.Top);sourcePanel.Children.Add(sourceBar);sourcePanel.Children.Add(sourceHost);modes.Items.Add(new TabItem{Header="XAML",Content=sourcePanel});
        split.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});split.RowDefinitions.Add(new RowDefinition{Height=new GridLength(5)});split.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
        modes.Items.Add(new TabItem{Header="Split",Content=split});
        var handlerPanel=new DockPanel();var handlerBar=new StackPanel{Orientation=Orientation.Horizontal};handlerBar.Children.Add(handlerTitle);handlerBar.Children.Add(handlerList);handlerBar.Children.Add(ActionButton("Apply handler",SaveHandler));DockPanel.SetDock(handlerBar,Dock.Top);handlerPanel.Children.Add(handlerBar);handlerPanel.Children.Add(HandlerEditor);modes.Items.Add(new TabItem{Header="PowerShell events",Content=handlerPanel});
        modes.SelectionChanged+=(s,e)=>{if(e.Source==modes)ArrangeMode();};
        handlerList.SelectionChanged+=(s,e)=>{if(!syncing&&handlerList.SelectedItem is string ev&&eventControl is not null)EditEvent(eventControl,ev);};
        Canvas.SelectionChanged+=Select;Canvas.Error+=Report;Canvas.ControlDropped+=(t,p,x,y)=>AddControl(t,p,x,y);
        Inspector.ApplyProperty=(name,property,value)=>{var changed=Change(d=>d.SetProperty(name,property,value));if(changed)Select(property=="Name"?value!:name);return changed;};
        Inspector.EventSelected+=EditEvent;
        Inspector.XamlRequested+=()=>{modes.SelectedIndex=1;Source.Focus();};
        Document.Changed+=()=>{Refresh();Changed?.Invoke();};
        Source.TextChanged+=(s,e)=>{if(syncing)return;sourceDirty=true;Changed?.Invoke();parseTimer.Stop();parseTimer.Start();};
        HandlerEditor.TextChanged+=(s,e)=>{if(!syncing){handlerDirty=true;Changed?.Invoke();}};
        parseTimer.Tick+=(s,e)=>{parseTimer.Stop();ApplySource();};
        Source.Suggestions=()=>Document.Elements.Select(e=>e.Name.LocalName).Concat(new[]{"Grid","Button","TextBox","TextBlock","StackPanel","Width","Height","Margin","Background","Binding","StaticResource","x:Name"});
        HandlerEditor.Suggestions=()=>Document.Elements.Select(e=>"$"+DesignDocument.NameOf(e)).Concat(new[]{"$sender","$eventArgs","$window","Write-Host","Get-ChildItem","Get-Service","Start-Process","[System.Windows.MessageBox]::Show"});
        PreviewKeyDown+=KeyDownHandler;Refresh();ArrangeMode();Select("MainWindow");
    }
    public static Button ActionButton(string text,Action action){var b=new Button{Content=text,Margin=new Thickness(4),Padding=new Thickness(10,4,10,4)};b.Click+=(s,e)=>action();return b;}
    void ArrangeMode()
    {
        canvasHost.Content=null;sourceHost.Content=null;split.Children.Clear();SetRow(Canvas,0);SetRow(Source,0);
        if(modes.SelectedIndex==2){split.Children.Add(Canvas);var splitter=new GridSplitter{Height=5,HorizontalAlignment=HorizontalAlignment.Stretch,Background=Brushes.LightGray};SetRow(splitter,1);split.Children.Add(splitter);SetRow(Source,2);split.Children.Add(Source);}
        else {canvasHost.Content=Canvas;sourceHost.Content=Source;}
    }
    public void MarkSaved(){saved=Document.Serialize();Changed?.Invoke();}
    public bool ApplySource()
    {
        if(!sourceDirty)return true;
        try{SaveHandler();var draft=DesignDocument.Deserialize(Document.Serialize());draft.ApplyXaml(Source.Text);DesignRenderer.Render(draft);sourceDirty=false;Document.ApplyXaml(Source.Text);Report("Ready");return true;}
        catch(Exception e){Report("XAML: "+e.Message);return false;}
    }
    public bool Prepare(){SaveHandler();return ApplySource();}
    public bool Change(Action<DesignDocument> action)
    {
        if(!ApplySource())return false;try{SaveHandler();Document.Transact(action,d=>DesignRenderer.Render(d));Report("Ready");return true;}catch(Exception e){Report(e.Message);return false;}
    }
    public void AddControl(string type,string? parent=null,double x=24,double y=24)
    {
        parent??=InsertionParent();string name="";if(Change(d=>name=d.Add(type,parent,x,y))){modes.SelectedIndex=0;Canvas.Select(name);}
    }
    string InsertionParent()
    {
        var selected=Canvas.Selected;var e=Document.Elements.FirstOrDefault(e=>DesignDocument.NameOf(e)==selected)??Document.Xml.Root;
        while(e is not null){if(e.Name.LocalName is "Grid" or "Canvas" or "StackPanel" or "WrapPanel" or "DockPanel" or "Border" or "GroupBox" or "Expander" or "TabItem" or "TabControl" or "ScrollViewer" or "Viewbox")return DesignDocument.NameOf(e);e=e.Parent;}
        return Document.Elements.FirstOrDefault(e=>e.Name.LocalName is "Grid" or "Canvas" or "StackPanel") is {} panel?DesignDocument.NameOf(panel):DesignDocument.NameOf(Document.Xml.Root!);
    }
    public void Select(string name)
    {
        if(!Document.Elements.Any(e=>DesignDocument.NameOf(e)==name))return;
        Inspector.Inspect(Document,name,name==DesignDocument.NameOf(Document.Xml.Root!)?Canvas.Rendered?.RootWindow:Canvas.Rendered?.Controls.GetValueOrDefault(name));UiAppearance.Apply(Inspector);
    }
    void Refresh()
    {
        syncing=true;try{
            if(!sourceDirty&&Source.Text!=Document.Xaml){var caret=Source.CaretOffset;Source.Text=Document.Xaml;Source.CaretOffset=Math.Min(caret,Source.Text.Length);}
            Canvas.Render();RefreshTree();if(Canvas.Selected is {} n&&Document.Elements.Any(e=>DesignDocument.NameOf(e)==n))Select(n);
            if(!handlerDirty&&eventControl is not null&&eventName is not null){
                if(Document.Elements.Any(e=>DesignDocument.NameOf(e)==eventControl)){HandlerEditor.Text=Document.Events.FirstOrDefault(e=>e.ControlName==eventControl&&e.EventName==eventName)?.Code??"";}
                else {eventControl=eventName=null;handlerTitle.Text="Select an event in Properties";handlerList.Items.Clear();HandlerEditor.Text="";}
            }
        }finally{syncing=false;}UiAppearance.Apply(this);
    }
    void RefreshTree()
    {
        Hierarchy.Items.Clear();TreeViewItem Build(XElement e){var n=DesignDocument.NameOf(e);var item=new TreeViewItem{Header=e.Name.LocalName+(n.Length>0?"  ·  "+n:""),IsExpanded=true,Tag=n};item.Selected+=(s,a)=>{if(n.Length>0){Canvas.Select(n);a.Handled=true;}};foreach(var child in e.Elements().Where(DesignDocument.IsDesignElement))item.Items.Add(Build(child));return item;}Hierarchy.Items.Add(Build(Document.Xml.Root!));
    }
    public void EditEvent(string name,string ev)
    {
        SaveHandler();eventControl=name;eventName=ev;syncing=true;
        handlerTitle.Text=name+" ·";handlerList.Items.Clear();var type=Canvas.Rendered?.Controls.GetValueOrDefault(name)?.GetType()??typeof(Window);
        foreach(var item in type.GetEvents().Select(e=>e.Name).OrderBy(n=>n))handlerList.Items.Add(item);handlerList.SelectedItem=ev;
        HandlerEditor.Text=Document.Events.FirstOrDefault(e=>e.ControlName==name&&e.EventName==ev)?.Code??"# $sender is the control that raised this event.\n# Access named controls directly, for example: $TextBox1.Text\n";syncing=false;handlerDirty=false;modes.SelectedIndex=3;
    }
    public void SaveHandler(){if(!handlerDirty||eventControl is null||eventName is null)return;var code=HandlerEditor.Text;handlerDirty=false;try{Document.SetEvent(eventControl,eventName,code);}catch{handlerDirty=true;throw;}}
    public void Undo(){if(modes.SelectedIndex==1&&Source.IsKeyboardFocusWithin){Source.Undo();return;}SaveHandler();Document.Undo();}
    public void Redo(){if(modes.SelectedIndex==1&&Source.IsKeyboardFocusWithin){Source.Redo();return;}Document.Redo();}
    public void DeleteSelection(){if(Canvas.Selected is {} name)Change(d=>d.Remove(name));}
    public void Copy(){copyName=Canvas.Selected;Report(copyName is null?"Select a control first.":"Copied "+copyName);}
    public void Paste(){if(copyName is null)return;string result="";if(Change(d=>result=d.Duplicate(copyName,InsertionParent())))Canvas.Select(result);}
    public void ShowMode(int mode)=>modes.SelectedIndex=mode;
    void KeyDownHandler(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Enter&&Keyboard.Modifiers==ModifierKeys.Control){Prepare();e.Handled=true;return;}
        if(!Canvas.IsKeyboardFocusWithin)return;
        if(e.Key==Key.Delete){DeleteSelection();e.Handled=true;}
        if(Keyboard.Modifiers==ModifierKeys.Control){if(e.Key==Key.C){Copy();e.Handled=true;}if(e.Key==Key.V){Paste();e.Handled=true;}if(e.Key==Key.Z){Undo();e.Handled=true;}if(e.Key==Key.Y){Redo();e.Handled=true;}}
        var step=Keyboard.Modifiers==ModifierKeys.Shift?10:1;
        if(e.Key is Key.Left or Key.Right or Key.Up or Key.Down){Canvas.MoveSelection(e.Key==Key.Left?-step:e.Key==Key.Right?step:0,e.Key==Key.Up?-step:e.Key==Key.Down?step:0);e.Handled=true;}
    }
    void Report(string message)=>Status?.Invoke(message);
}
