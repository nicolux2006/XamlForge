using System.Xml.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
namespace XamlForge.Core;
public sealed record EventBinding(string ControlName, string EventName, string Code);
public sealed class DesignDocument
{
    public static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    public static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    public XDocument Xml { get; private set; } = new();
    public List<EventBinding> Events { get; private set; } = [];
    public List<string> Assemblies { get; private set; } = [];
    public string Xaml => Xml.ToString();
    public event Action? Changed;
    readonly Stack<string> undo = new(), redo = new();
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public static string NameOf(XElement e) => (string?)e.Attribute(X+"Name") ?? (string?)e.Attribute("Name") ?? "";
    public static bool IsElement(XElement e) => !e.Name.LocalName.Contains('.');
    public static bool IsDesignElement(XElement e) => IsElement(e)&&!e.AncestorsAndSelf().Any(a=>a.Name.LocalName.EndsWith("Template")||a.Name.LocalName.EndsWith(".Resources")||a.Name.LocalName is "Style" or "ResourceDictionary");
    public IEnumerable<XElement> Elements => Xml.Root!.DescendantsAndSelf().Where(IsDesignElement);
    public DesignDocument()
    {
        Xml=XDocument.Parse("<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='MainWindow' Title='PowerShell Window' Width='640' Height='480' Background='White'><Grid x:Name='LayoutRoot'/></Window>");
    }
    static void Identifier(string value)
    {
        if (!Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*$")) throw new ArgumentException("Use a name beginning with a letter or underscore, followed by letters, digits or underscores.");
        if (new[]{"window","sender","eventArgs","controls","Error","args","input","this","true","false","null","Host","PSVersionTable","PID","HOME","PSHOME","ExecutionContext","ShellId","PSCulture","PSUICulture","EnabledExperimentalFeatures","IsCoreCLR","IsLinux","IsMacOS","IsWindows","PSEdition","PSStyle","PSCmdlet","MyInvocation","PSBoundParameters","PSCommandPath","PSScriptRoot","ErrorActionPreference","WarningPreference","InformationPreference","VerbosePreference","DebugPreference","ProgressPreference","ConfirmPreference","WhatIfPreference","OFS","OutputEncoding","LASTEXITCODE","Matches","foreach","switch"}.Contains(value,StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("This name is reserved by PowerShell. Choose a different control name.");
    }
    static void Validate(XDocument xml)
    {
        if(xml.Root?.Name != Wpf+"Window") throw new ArgumentException("The root element must be a WPF Window.");
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var e in xml.Root.DescendantsAndSelf().Where(IsDesignElement)) {var name=NameOf(e);if(name.Length==0)continue;Identifier(name);if(!names.Add(name))throw new ArgumentException("Duplicate control name: "+name);}
        if(xml.Descendants().Attributes(X+"Class").Any()) throw new ArgumentException("Remove x:Class: standalone PowerShell XAML cannot use a compiled code-behind class.");
    }
    void Edit(Action operation)
    {
        var before=Serialize();
        try {operation();Validate(Xml);} catch {Restore(before);throw;}
        if(before==Serialize())return;
        undo.Push(before);redo.Clear();Changed?.Invoke();
    }
    public string Add(string type, string parent = "LayoutRoot", double x = 20, double y = 20)
    {
        if(!Regex.IsMatch(type,@"^[A-Za-z][A-Za-z0-9]*$"))throw new ArgumentException("Invalid control type.");
        var p=Find(parent); var local=p.Name.LocalName;
        if(!new[]{"Grid","Canvas","StackPanel","WrapPanel","DockPanel","UniformGrid","Window","Border","GroupBox","Expander","ScrollViewer","TabControl","TabItem","Viewbox"}.Contains(local)) throw new ArgumentException("Select a layout container first.");
        if(new[]{"Window","Border","GroupBox","Expander","ScrollViewer","TabItem","Viewbox"}.Contains(local)&&p.Elements().Any(IsElement))throw new ArgumentException("This container already has content. Insert into its child panel.");
        string name=type+1;for(int i=2;Elements.Any(e=>NameOf(e).Equals(name,StringComparison.OrdinalIgnoreCase));i++)name=type+i;
        var element=new XElement(Wpf+type,new XAttribute(X+"Name",name));
        if(type is "Button" or "Label" or "CheckBox" or "RadioButton" or "ToggleButton" or "RepeatButton")element.SetAttributeValue("Content",type);
        if(type is "TextBlock" or "TextBox" or "Run")element.SetAttributeValue("Text",type);
        if(type is "GroupBox" or "Expander" or "TabItem")element.SetAttributeValue("Header",type);
        if(type is "Grid" or "Canvas" or "StackPanel" or "WrapPanel" or "DockPanel" or "Border") {element.SetAttributeValue("Width","260");element.SetAttributeValue("Height","180");element.SetAttributeValue("Background","#FFF4F6F8");}
        else if(type is not "TabItem") {element.SetAttributeValue("Width",type is "DataGrid" or "ListView" or "ListBox" or "TreeView" or "Calendar"?"220":"120");element.SetAttributeValue("Height",type is "DataGrid" or "ListView" or "ListBox" or "TreeView" or "Calendar" or "RichTextBox" or "TabControl"?"120":"32");}
        if(local=="Canvas") {element.SetAttributeValue("Canvas.Left",Number(x));element.SetAttributeValue("Canvas.Top",Number(y));}
        else if(local is "Grid") {element.SetAttributeValue("Margin",$"{Number(x)},{Number(y)},0,0");element.SetAttributeValue("HorizontalAlignment","Left");element.SetAttributeValue("VerticalAlignment","Top");}
        else element.SetAttributeValue("Margin","4");
        if(type=="TabControl")element.Add(new XElement(Wpf+"TabItem",new XAttribute("Header","Tab 1"),new XElement(Wpf+"Grid")));
        Edit(()=>p.Add(element));return name;
    }
    public static string Number(double n)=> Math.Round(n,1).ToString(CultureInfo.InvariantCulture);
    public void Remove(string name)
    {
        var e=Find(name);if(e==Xml.Root)throw new ArgumentException("The root Window cannot be removed.");
        var names=e.DescendantsAndSelf().Select(NameOf).ToHashSet();Edit(()=>{e.Remove();Events.RemoveAll(h=>names.Contains(h.ControlName));});
    }
    public void SetProperty(string name, string property, string? value)
    {
        if(property is "Name" or "x:Name"){Rename(name,value??"");return;}
        if(!Regex.IsMatch(property,@"^[A-Za-z_][A-Za-z0-9_.]*$"))throw new ArgumentException("Invalid property name.");
        var e=Find(name);Edit(()=>e.SetAttributeValue(property,string.IsNullOrWhiteSpace(value)?null:value));
    }
    public void SetProperties(string name, IReadOnlyDictionary<string,string?> values)
    {
        var e=Find(name);Edit(()=>{foreach(var p in values)e.SetAttributeValue(p.Key,p.Value);});
    }
    public void Rename(string name, string replacement)
    {
        Identifier(replacement);var e=Find(name);
        if(Elements.Any(o=>o!=e&&NameOf(o).Equals(replacement,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Duplicate control name: "+replacement);
        Edit(()=>{e.Attribute("Name")?.Remove();e.SetAttributeValue(X+"Name",replacement);Events=Events.Select(h=>h.ControlName==name?h with{ControlName=replacement}:h).ToList();});
    }
    public XElement Find(string name) => Elements.FirstOrDefault(e=>NameOf(e)==name)??throw new ArgumentException("Control not found: "+name);
    public void ApplyXaml(string xaml)
    {
        var parsed=XDocument.Parse(xaml);Validate(parsed);AssignDesignNames(parsed);
        Edit(()=>{Xml=parsed;var names=Elements.Select(NameOf).ToHashSet();Events.RemoveAll(h=>!names.Contains(h.ControlName));});
    }
    static void AssignDesignNames(XDocument xml)
    {
        var used=xml.Descendants().Select(NameOf).Where(n=>n.Length>0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var e in xml.Root!.DescendantsAndSelf().Where(IsElement))
        {
            if(NameOf(e).Length>0||e.Name.LocalName.EndsWith("Definition")||e.AncestorsAndSelf().Any(a=>a.Name.LocalName.Contains("Resources")||a.Name.LocalName.Contains("Template")||a.Name.LocalName is "Style" or "Triggers"))continue;
            var stem=e==xml.Root?"MainWindow":e.Name.LocalName;var name=e==xml.Root?stem:stem+1;for(var i=2;used.Contains(name);i++)name=stem+i;
            e.SetAttributeValue(X+"Name",name);used.Add(name);
        }
    }
    public void SetEvent(string name, string eventName, string code)
    {
        Find(name);Identifier(eventName);Edit(()=>{Events.RemoveAll(e=>e.ControlName==name&&e.EventName==eventName);if(!string.IsNullOrWhiteSpace(code))Events.Add(new(name,eventName,code));});
    }
    public bool Undo(){if(!undo.TryPop(out var previous))return false;redo.Push(Serialize());Restore(previous);Changed?.Invoke();return true;}
    public bool Redo(){if(!redo.TryPop(out var next))return false;undo.Push(Serialize());Restore(next);Changed?.Invoke();return true;}
    sealed record ProjectData(int Version,string Xaml,List<EventBinding> Events,List<string> Assemblies);
    public string Serialize()=>JsonSerializer.Serialize(new ProjectData(1,Xaml,Events,Assemblies),new JsonSerializerOptions{WriteIndented=true});
    void Restore(string json)
    {
        var data=JsonSerializer.Deserialize<ProjectData>(json)??throw new ArgumentException("Empty project.");
        if(data.Version!=1)throw new ArgumentException("Unsupported project version.");
        var xml=XDocument.Parse(data.Xaml);Validate(xml);Xml=xml;Events=data.Events??[];Assemblies=data.Assemblies??[];
    }
    public static DesignDocument Deserialize(string json){var d=new DesignDocument();d.Restore(json);foreach(var h in d.Events){d.Find(h.ControlName);Identifier(h.EventName);}return d;}
    public void Transact(Action<DesignDocument> change,Action<DesignDocument>? validate=null)
    {
        var draft=Deserialize(Serialize());change(draft);validate?.Invoke(draft);Edit(()=>Restore(draft.Serialize()));
    }
    public string Duplicate(string name, string parent)
    {
        var source=new XElement(Find(name));var mapping=new Dictionary<string,string>();var used=Elements.Select(NameOf).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach(var e in source.DescendantsAndSelf().Where(IsElement)){var old=NameOf(e);var stem=e.Name.LocalName;var n=stem+1;for(var i=2;used.Contains(n);i++)n=stem+i;used.Add(n);e.Attribute("Name")?.Remove();e.SetAttributeValue(X+"Name",n);if(old.Length>0)mapping[old]=n;}
        var p=Find(parent);if(p.Name.LocalName is not ("Grid" or "Canvas" or "StackPanel" or "WrapPanel" or "DockPanel"))throw new ArgumentException("Paste requires a layout panel.");
        Edit(()=>{p.Add(source);Events.AddRange(Events.Where(e=>mapping.ContainsKey(e.ControlName)).Select(e=>e with{ControlName=mapping[e.ControlName]}).ToList());});return NameOf(source);
    }
}
