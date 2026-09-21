using System.Windows;
using System.Windows.Controls;
using XamlForge.Core;
using XamlForge.Designer;

namespace XamlForge.App;

public sealed class EditorDialog : Window
{
    public int SelectedAction { get; private set; }
    public EditorDialog(Window owner, string title, UIElement content, params (string Label, int Result)[] actions)
    {
        Owner=owner;Title=UiText.Get(title);ShowInTaskbar=false;ResizeMode=ResizeMode.NoResize;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;SizeToContent=SizeToContent.WidthAndHeight;
        MinWidth=420;MaxWidth=660;FontFamily=owner.FontFamily;FontSize=12;
        var root=new DockPanel();Content=root;
        var footer=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(16,10,16,12)};
        foreach(var action in actions){
            var button=new Button{Content=action.Label,MinWidth=95,Padding=new Thickness(12,5,12,5),Margin=new Thickness(6,0,0,0),IsDefault=action.Result==1,IsCancel=action.Result==0};
            button.Click+=(_,_)=>{SelectedAction=action.Result;DialogResult=action.Result!=0;};footer.Children.Add(button);
        }
        DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        root.Children.Add(new Border{Padding=new Thickness(20),Child=content});
        UiAppearance.Apply(root);Background=UiAppearance.Surface;Foreground=UiAppearance.Text;
        Resources[SystemColors.WindowBrushKey]=UiAppearance.Surface;
        Resources[SystemColors.WindowTextBrushKey]=UiAppearance.Text;
    }
}
