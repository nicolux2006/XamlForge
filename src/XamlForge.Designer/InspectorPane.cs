using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using XamlForge.Core;

namespace XamlForge.Designer;
public sealed class InspectorPane : DockPanel
{
    readonly TextBlock typeText=new(){Margin=new Thickness(8),Foreground=Brushes.DimGray};
    readonly TextBox nameBox=new(){Margin=new Thickness(8,0,8,8)};
    readonly TextBox filter=new(){Margin=new Thickness(8),ToolTip="Filter properties"};
    readonly StackPanel rows=new();readonly ListBox events=new(){BorderThickness=new Thickness(0)};
    DesignDocument? document;string? selected;FrameworkElement? element;bool loading;int generation;
    public Func<string,string,string?,bool>? ApplyProperty {get;set;}
    public event Action<string,string>? EventSelected;
    public event Action? XamlRequested;
    public IReadOnlyList<string> PropertyNames {get;private set;} = [];
    public InspectorPane()
    {
        Background=Brushes.White;LastChildFill=true;
        var head=new StackPanel();head.Children.Add(new TextBlock{Text="Properties",FontWeight=FontWeights.SemiBold,Margin=new Thickness(8)});head.Children.Add(typeText);head.Children.Add(nameBox);DockPanel.SetDock(head,Dock.Top);Children.Add(head);
        var tabs=new TabControl{BorderThickness=new Thickness(0)};var properties=new DockPanel();DockPanel.SetDock(filter,Dock.Top);properties.Children.Add(filter);properties.Children.Add(new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        tabs.Items.Add(new TabItem{Header="Properties",Content=properties});tabs.Items.Add(new TabItem{Header="ϟ Events",Content=events});Children.Add(tabs);
        filter.TextChanged+=(s,e)=>Populate();nameBox.LostKeyboardFocus+=(s,e)=>{if(!loading&&selected is not null&&nameBox.Text!=selected)ApplyProperty?.Invoke(selected,"Name",nameBox.Text);};
        events.MouseDoubleClick+=(s,e)=>{if(selected is not null&&events.SelectedItem is string ev)EventSelected?.Invoke(selected,ev);};
        events.KeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Enter&&selected is not null&&events.SelectedItem is string ev)EventSelected?.Invoke(selected,ev);};
    }
    public void Inspect(DesignDocument doc,string name,FrameworkElement? control)
    {
        loading=true;document=doc;selected=name;element=control;nameBox.Text=name;typeText.Text="Type: "+doc.Find(name).Name.LocalName;loading=false;Populate();
    }
    void Populate()
    {
        if(document is null||selected is null||loading)return;loading=true;
        try {
            var currentGeneration=++generation;var currentName=selected;rows.Children.Clear();events.Items.Clear();var xml=document.Find(selected);var type=xml==document.Xml.Root?typeof(Window):element?.GetType();
            if(type is null)return;
            var parentType=element?.Parent?.GetType();
            var descriptors=PropertyCatalog.For(type).Where(p=>PropertyCatalog.IsUseful(p,parentType) || PropertyCatalog.CanEdit(p) && xml.Attribute(p.Name) is not null).ToDictionary(p=>p.Name);
            var favorites=new[]{"Width","Height","MinWidth","MinHeight","Margin","Padding","HorizontalAlignment","VerticalAlignment","Content","Text","Title","Background","Foreground","FontSize","FontFamily","FontWeight","IsEnabled","Visibility","Orientation"};
            var names=favorites.Where(descriptors.ContainsKey).Concat(xml.Attributes().Where(a=>!a.IsNamespaceDeclaration&&a.Name.NamespaceName.Length==0&&a.Name.LocalName!="Name").Select(a=>a.Name.LocalName)).Concat(descriptors.Keys.OrderBy(x=>x)).Distinct();
            PropertyNames=names.ToArray();
            foreach(var property in names.Where(n=>n.Contains(filter.Text,StringComparison.OrdinalIgnoreCase)))
            {
                var value=(string?)xml.Attribute(property);
                if(value is null&&element is not null&&descriptors.TryGetValue(property,out var pd)) {try{value=TypeDescriptor.GetConverter(pd.Type).ConvertToInvariantString(pd.Read(element));}catch{value="";}}
                var line=new Grid{Margin=new Thickness(8,2,8,2)};line.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(0.8,GridUnitType.Star)});line.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1.2,GridUnitType.Star)});
                line.Children.Add(new TextBlock{Text=property,VerticalAlignment=VerticalAlignment.Center,FontSize=11,ToolTip=property,TextTrimming=TextTrimming.CharacterEllipsis});
                descriptors.TryGetValue(property,out var descriptor);
                FrameworkElement editor;
                if(descriptor?.ReadOnly==true || descriptor?.TextEditable==false)
                {
                    var details=new DockPanel();
                    if(descriptor.TextEditable==false){var xaml=new Button{Content="XAML…",Padding=new Thickness(4),ToolTip="Edit complex value in XAML"};xaml.Click+=(_,_)=>XamlRequested?.Invoke();DockPanel.SetDock(xaml,Dock.Right);details.Children.Add(xaml);}
                    details.Children.Add(new TextBox{Text=string.IsNullOrEmpty(value)?UiText.Get(descriptor.TextEditable?"Read-only":"Complex value"):value,IsReadOnly=true,MinHeight=28,Padding=new Thickness(4),ToolTip=UiText.Get(descriptor.ReadOnly?"Read-only":"Edit complex value in XAML")});editor=details;
                }
                else editor=new PropertyValueEditor(property,descriptor?.Type,value??"",next=>
                    !loading && generation==currentGeneration && selected==currentName && (ApplyProperty?.Invoke(currentName,property,next)??false));
                Grid.SetColumn(editor,1);line.Children.Add(editor);rows.Children.Add(line);
            }
            foreach(var ev in type.GetEvents().OrderBy(e=>e.Name))events.Items.Add(ev.Name);
        } finally{loading=false;UiAppearance.Apply(this);}
    }
}
