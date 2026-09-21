using System.Diagnostics;
using System.Windows.Documents;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using XamlForge.Core;

namespace XamlForge.Designer;
public sealed class Workspace : Grid, IDisposable
{
    public EditorCommandBar Commands {get;}
    public TabControl Documents {get;}=new(){BorderThickness=new Thickness(0)};
    readonly TextBlock status=new(){Text="Ready",Foreground=Brushes.White,Margin=new Thickness(8,4,8,4)};
    readonly TextBox output=new(){IsReadOnly=true,TextWrapping=TextWrapping.Wrap,BorderThickness=new Thickness(0),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,FontFamily=new FontFamily("Consolas"),FontSize=11};
    readonly ListBox toolbox=new(){BorderThickness=new Thickness(0)};readonly TextBox filter=new(){Margin=new Thickness(6),ToolTip="Filter WPF controls"};
    readonly ContentControl hierarchy=new();readonly ListBox files=new(){BorderThickness=new Thickness(0)};
    readonly string[] common=["Border","Button","Calendar","Canvas","CheckBox","ComboBox","DataGrid","DatePicker","DockPanel","Expander","Grid","GridSplitter","GroupBox","Image","Label","ListBox","ListView","MediaElement","PasswordBox","ProgressBar","RadioButton","Rectangle","RepeatButton","RichTextBox","ScrollViewer","Separator","Slider","StackPanel","TabControl","TextBlock","TextBox","ToggleButton","TreeView","Viewbox","WrapPanel"];
    Point dragStart;Process? running;int counter;string? tempScript;
    public DesignView? ActiveDesign=>(Documents.SelectedItem as TabItem)?.Content as DesignView;
    public event Action<string>? StatusChanged;
    public Func<string,Task<SaveDecision>>? ConfirmSaveAsync {get;set;}
    bool closePending;
    public Workspace()
    {
        TextElement.SetFontFamily(this,new FontFamily("Segoe UI"));TextElement.SetFontSize(this,12);Background=new SolidColorBrush(Color.FromRgb(243,243,243));
        RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
        var body=new Grid();body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(205),MinWidth=140});body.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(5)});body.ColumnDefinitions.Add(new ColumnDefinition());Children.Add(body);
        var leftTabs=new TabControl{TabStripPlacement=Dock.Bottom,BorderThickness=new Thickness(0)};body.Children.Add(leftTabs);
        var tools=new DockPanel{Background=Brushes.White};var header=new StackPanel();header.Children.Add(new TextBlock{Text="Toolbox",FontWeight=FontWeights.SemiBold,Margin=new Thickness(8)});header.Children.Add(filter);
        var all=new CheckBox{Content="All WPF controls",Margin=new Thickness(8,2,8,6)};header.Children.Add(all);DockPanel.SetDock(header,Dock.Top);tools.Children.Add(header);tools.Children.Add(toolbox);
        void Fill(){var types=all.IsChecked==true?typeof(Button).Assembly.GetExportedTypes().Where(t=>typeof(FrameworkElement).IsAssignableFrom(t)&&!t.IsAbstract&&t.GetConstructor(Type.EmptyTypes)!=null).Select(t=>t.Name).Concat(common).Distinct().OrderBy(n=>n):common.AsEnumerable();toolbox.ItemsSource=types.Where(n=>n.Contains(filter.Text,StringComparison.OrdinalIgnoreCase)).ToList();}
        filter.TextChanged+=(s,e)=>Fill();all.Checked+=(s,e)=>Fill();all.Unchecked+=(s,e)=>Fill();Fill();
        toolbox.MouseDoubleClick+=(s,e)=>{if(toolbox.SelectedItem is string type)ActiveDesign?.AddControl(type);};toolbox.PreviewMouseLeftButtonDown+=(s,e)=>dragStart=e.GetPosition(toolbox);
        toolbox.PreviewMouseMove+=(s,e)=>{if(e.LeftButton==MouseButtonState.Pressed&&toolbox.SelectedItem is string type&&(e.GetPosition(toolbox)-dragStart).Length>6)DragDrop.DoDragDrop(toolbox,new DataObject("XamlForge.Control",type),DragDropEffects.Copy);};
        leftTabs.Items.Add(new TabItem{Header="Toolbox",Content=tools});leftTabs.Items.Add(new TabItem{Header="Files",Content=files});leftTabs.Items.Add(new TabItem{Header="Outline",Content=hierarchy});
        files.SelectionChanged+=(s,e)=>{if(files.SelectedIndex>=0&&files.SelectedIndex<Documents.Items.Count)Documents.SelectedIndex=files.SelectedIndex;};
        var splitter=new GridSplitter{Width=5,HorizontalAlignment=HorizontalAlignment.Stretch,Background=new SolidColorBrush(Color.FromRgb(216,220,226))};SetColumn(splitter,1);body.Children.Add(splitter);SetColumn(Documents,2);body.Children.Add(Documents);
        Documents.SelectionChanged+=(s,e)=>{if(e.Source==Documents){hierarchy.Content=ActiveDesign?.Hierarchy;RefreshFiles();}};
        var errors=new Expander{Header="Output / Error List",Content=output,MaxHeight=180};output.MinHeight=90;SetRow(errors,1);Children.Add(errors);
        var footer=new Border{Background=new SolidColorBrush(Color.FromRgb(0,122,204)),Child=status};SetRow(footer,2);Children.Add(footer);
        PreviewKeyDown+=(s,e)=>{if(Keyboard.Modifiers==ModifierKeys.Control){if(e.Key==Key.S){Save();e.Handled=true;}if(e.Key==Key.O){Open();e.Handled=true;}if(e.Key==Key.N){New();e.Handled=true;}}if(e.Key==Key.F5){Run();e.Handled=true;}};
        RowDefinitions.Insert(0,new RowDefinition{Height=GridLength.Auto});
        foreach(UIElement child in Children)SetRow(child,GetRow(child)+1);
        Commands=new EditorCommandBar(this);Children.Add(Commands);
        New();UiAppearance.Apply(this);
    }
    public void New()=>AddDesign(new DesignDocument(),"Untitled"+(++counter)+".xfg");
    public DesignView AddDesign(DesignDocument doc,string title,string? path=null)
    {
        var view=new DesignView(doc){Title=title,FilePath=path};var tab=new TabItem{Content=view};
        view.Changed+=()=>{UpdateHeader(tab,view.Title+(view.Dirty?" *":""));RefreshFiles();};view.Status+=Report;UpdateHeader(tab,title);Documents.Items.Add(tab);Documents.SelectedItem=tab;RefreshFiles();UiAppearance.Apply(this);return view;
    }
    void UpdateHeader(TabItem tab,string title)
    {
        var header=new StackPanel{Orientation=Orientation.Horizontal};header.Children.Add(new TextBlock{Text=title,Margin=new Thickness(4,3,8,3)});var close=new Button{Content="×",Padding=new Thickness(3,0,3,0),BorderThickness=new Thickness(0),Background=Brushes.Transparent,ToolTip="Close document"};close.Click+=async(s,e)=>await CloseTabAsync(tab);header.Children.Add(close);tab.Header=header;tab.Tag=title;UiAppearance.Apply(header);
    }
    public async Task<bool> CloseTabAsync(TabItem tab)
    {
        if(closePending||!Documents.Items.Contains(tab))return false;
        closePending=true;
        try {if(!await ConfirmTabAsync(tab))return false;var index=Documents.Items.IndexOf(tab);Documents.Items.Remove(tab);if(Documents.SelectedIndex<0&&Documents.Items.Count>0)Documents.SelectedIndex=Math.Min(index,Documents.Items.Count-1);RefreshFiles();return true;}
        finally {closePending=false;}
    }
    static bool IsDirty(TabItem tab)=>tab.Content switch {DesignView d=>d.Dirty,ScriptView s=>s.Dirty,_=>false};
    async Task<bool> ConfirmTabAsync(TabItem tab)
    {
        if(!IsDirty(tab))return true;
        Documents.SelectedItem=tab;
        var title=tab.Content switch{DesignView d=>d.Title,ScriptView s=>s.Title,_=>"Document"};
        var answer=ConfirmSaveAsync is null?SaveDecision.Cancel:await ConfirmSaveAsync(title);
        if(answer==SaveDecision.Cancel)return false;
        if(answer==SaveDecision.Discard)return true;
        Documents.SelectedItem=tab;
        return Save();
    }
    public async Task<bool> CanCloseAsync()
    {
        if(closePending)return false;
        closePending=true;
        try {foreach(var tab in Documents.Items.Cast<TabItem>().ToArray())if(!await ConfirmTabAsync(tab))return false;return true;}
        finally {closePending=false;}
    }
    void RefreshFiles(){var selected=Documents.SelectedIndex;files.ItemsSource=Documents.Items.Cast<TabItem>().Select(t=>t.Tag?.ToString()??"Document").ToList();files.SelectedIndex=selected;}
    public void Open()
    {
        var dialog=new OpenFileDialog{Filter=UiText.Get("XamlForge, XAML or PowerShell|*.xfg;*.xaml;*.ps1|All files|*.*"),Multiselect=true};if(dialog.ShowDialog()!=true)return;
        foreach(var path in dialog.FileNames)Try(()=>OpenPath(path));
    }
    public void OpenPath(string path)
    {
        var text=File.ReadAllText(path);if(Path.GetExtension(path).Equals(".ps1",StringComparison.OrdinalIgnoreCase)){AddScript(text,Path.GetFileName(path),path,false);return;}
        var doc=Path.GetExtension(path).Equals(".xfg",StringComparison.OrdinalIgnoreCase)?DesignDocument.Deserialize(text):new DesignDocument();if(Path.GetExtension(path).Equals(".xaml",StringComparison.OrdinalIgnoreCase))doc.ApplyXaml(text);DesignRenderer.Render(doc);AddDesign(doc,Path.GetFileName(path),path);
    }
    public bool Save(bool saveAs=false)
    {
        var tab=Documents.SelectedItem as TabItem;if(tab is null)return false;
        if(tab.Content is DesignView view)
        {
            if(!view.Prepare())return false;var path=view.FilePath;
            if(saveAs||path is null){var dlg=new SaveFileDialog{Filter=UiText.Get("XamlForge project (includes events)|*.xfg|WPF XAML (layout only)|*.xaml"),FileName=path is null?Path.ChangeExtension(view.Title,"xfg"):Path.GetFileName(path)};if(dlg.ShowDialog()!=true)return false;path=dlg.FileName;}
            // A XAML-only file cannot persist event scripts. Offer a project instead.
            if(Path.GetExtension(path).Equals(".xaml",StringComparison.OrdinalIgnoreCase)&&view.Document.Events.Count>0){Report("Use Save As → XamlForge project to preserve event handlers. Use Export for a standalone script.");return false;}
            if(!Try(()=>File.WriteAllText(path,Path.GetExtension(path).Equals(".xaml",StringComparison.OrdinalIgnoreCase)?view.Document.Xaml:view.Document.Serialize(),new UTF8Encoding(true))))return false;
            view.FilePath=path;view.Title=Path.GetFileName(path);view.MarkSaved();Report("Saved "+path);return true;
        }
        if(tab.Content is ScriptView script)
        {
            var path=script.FilePath;if(saveAs||path is null){var dlg=new SaveFileDialog{Filter=UiText.Get("PowerShell script|*.ps1"),FileName=Path.GetFileName(script.Title)};if(dlg.ShowDialog()!=true)return false;path=dlg.FileName;}
            if(!Try(()=>File.WriteAllText(path,script.Editor.Text,new UTF8Encoding(true))))return false;script.FilePath=path;script.Title=Path.GetFileName(path);script.MarkSaved();UpdateHeader(tab,script.Title);Report("Saved "+path);return true;
        }
        return false;
    }
    public void Export()
    {
        if(ActiveDesign is not {} view){Report("Select a design document to export.");return;}if(!view.Prepare())return;
        Try(()=>AddScript(PowerShellExporter.Export(view.Document),Path.GetFileNameWithoutExtension(view.Title)+".ps1",null,true));
    }
    public ScriptView AddScript(string text,string title,string? path=null,bool dirty=true)
    {
        var script=new ScriptView(text,title,path,dirty);var tab=new TabItem{Content=script};script.Editor.TextChanged+=(s,e)=>UpdateHeader(tab,script.Title+(script.Dirty?" *":""));UpdateHeader(tab,title+(dirty?" *":""));Documents.Items.Add(tab);Documents.SelectedItem=tab;RefreshFiles();return script;
    }
    public void Run()
    {
        if(running is {HasExited:false}){Report("A preview is already running. Stop it first.");return;}
        string code;if(ActiveDesign is {} view){if(!view.Prepare())return;code=PowerShellExporter.Export(view.Document);}else if((Documents.SelectedItem as TabItem)?.Content is ScriptView script)code=script.Editor.Text;else return;
        Try(()=>{var dir=Path.Combine(Path.GetTempPath(),"XamlForge");Directory.CreateDirectory(dir);tempScript=Path.Combine(dir,Guid.NewGuid()+".ps1");File.WriteAllText(tempScript,code,new UTF8Encoding(true));
            var start=new ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};foreach(var arg in new[]{"-NoProfile","-STA","-ExecutionPolicy","Bypass","-File",tempScript})start.ArgumentList.Add(arg);
            running=new Process{StartInfo=start,EnableRaisingEvents=true};running.OutputDataReceived+=(s,e)=>{if(e.Data is not null)Dispatcher.BeginInvoke(new Action(()=>AppendOutput(e.Data)));};running.ErrorDataReceived+=(s,e)=>{if(e.Data is not null)Dispatcher.BeginInvoke(new Action(()=>AppendOutput(e.Data)));};running.Exited+=(s,e)=>Dispatcher.BeginInvoke(new Action(()=>Report("Preview finished")));running.Start();running.BeginOutputReadLine();running.BeginErrorReadLine();Report("Running in Windows PowerShell (STA)");});
    }
    public void Stop(){if(running is {HasExited:false})running.Kill(true);Report("Stopped");}
    public void Undo()=>ActiveDesign?.Undo();public void Redo()=>ActiveDesign?.Redo();public void Copy()=>ActiveDesign?.Copy();public void Paste()=>ActiveDesign?.Paste();public void Delete()=>ActiveDesign?.DeleteSelection();
    public void Mode(int mode)=>ActiveDesign?.ShowMode(mode);
    public void ApplyPreferences(AppSettings settings,bool dark){UiAppearance.Configure(settings,dark);UiAppearance.Apply(this);}
    public void Report(string message){status.Text=message;UiAppearance.Apply(status);status.Foreground=Brushes.White;StatusChanged?.Invoke(message);if(message!="Ready")AppendOutput(UiText.Message(message));}
    void AppendOutput(string text){output.AppendText(text+Environment.NewLine);output.ScrollToEnd();}
    bool Try(Action action){try{action();return true;}catch(Exception e){Report(e.Message);return false;}}
    public void Dispose(){if(running is {HasExited:false})running.Kill(true);running?.Dispose();if(tempScript is not null&&File.Exists(tempScript))File.Delete(tempScript);}
}
public sealed class ScriptView : Grid
{
    public CodeEditor Editor {get;}=new(true);public string Title {get;set;}public string? FilePath {get;set;}string? saved;
    public bool Dirty=>saved!=Editor.Text;
    public ScriptView(string text,string title,string? path,bool dirty){Title=title;FilePath=path;Editor.Text=text;saved=dirty?null:text;Children.Add(Editor);}
    public void MarkSaved()=>saved=Editor.Text;
}



public enum SaveDecision { Cancel, Save, Discard }
