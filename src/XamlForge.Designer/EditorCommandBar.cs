using System.Windows;
using System.Windows.Controls;

namespace XamlForge.Designer;

/// <summary>The command surface belongs to WPF, so its menus coexist with the designer.</summary>
public sealed class EditorCommandBar : StackPanel
{
    public Menu MenuBar { get; } = new() { Padding = new Thickness(3, 1, 3, 1), FontSize = 13, MinHeight = 24 };
    public ItemsControl Toolbar { get; } = new() { Margin = new Thickness(4, 1, 4, 5), Padding = new Thickness(2), FontSize = 13, MinHeight = 38 };
    public event Action? SettingsRequested;
    public event Action? AboutRequested;
    public event Action? ExitRequested;

    public EditorCommandBar(Workspace workspace)
    {
        Children.Add(MenuBar);
        Toolbar.ItemsPanel = (ItemsPanelTemplate)System.Windows.Markup.XamlReader.Parse("<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><WrapPanel IsItemsHost='True'/></ItemsPanelTemplate>");
        Children.Add(Toolbar);
        AddMenu("File", ("New", workspace.New), ("Open…", workspace.Open), ("Save", () => workspace.Save()),
            ("Save As…", () => workspace.Save(true)), ("Export Design to PowerShell Script", workspace.Export), ("Exit", () => ExitRequested?.Invoke()));
        AddMenu("Edit", ("Undo", workspace.Undo), ("Redo", workspace.Redo), ("Copy control", workspace.Copy),
            ("Paste control", workspace.Paste), ("Delete control", workspace.Delete));
        AddMenu("View", ("Design", () => workspace.Mode(0)), ("XAML", () => workspace.Mode(1)),
            ("Split", () => workspace.Mode(2)), ("PowerShell events", () => workspace.Mode(3)));
        AddMenu("Run", ("Run / Preview   F5", workspace.Run), ("Stop", workspace.Stop));
        AddMenu("Settings", ("Settings", () => SettingsRequested?.Invoke()));
        AddMenu("Help", ("About XamlForge", () => AboutRequested?.Invoke()));
        AddButton("＋", "New", workspace.New);
        AddButton("▱", "Open", workspace.Open);
        AddButton("▣", "Save", () => workspace.Save());
        Separator();
        AddButton("↶", "Undo", workspace.Undo);
        AddButton("↷", "Redo", workspace.Redo);
        Separator();
        AddButton("▶", "Run / Preview   F5", workspace.Run);
        AddButton("■", "Stop", workspace.Stop);
        Separator();
        AddButton("Export to PowerShell", "Export Design to PowerShell Script", workspace.Export);
    }

    void AddMenu(string title, params (string Title, Action Execute)[] commands)
    {
        var menu = new MenuItem { Header = title, Padding = new Thickness(7, 3, 7, 3) };
        foreach (var command in commands)
        {
            var item = new MenuItem { Header = command.Title, Padding = new Thickness(8, 4, 8, 4) };
            item.Click += (_, _) => command.Execute();
            menu.Items.Add(item);
        }
        MenuBar.Items.Add(menu);
    }

    void AddButton(string content, string tip, Action execute)
    {
        var button = new Button { Content = content, ToolTip = tip, MinWidth = 32, Height = 32, FontSize = content.Length<=2?16:13,
            Padding = new Thickness(7, 3, 7, 3), Margin = new Thickness(2, 0, 2, 0) };
        System.Windows.Automation.AutomationProperties.SetName(button, tip);
        button.Click += (_, _) => execute();
        Toolbar.Items.Add(button);
    }

    void Separator() => Toolbar.Items.Add(new Separator { Width = 1, Margin = new Thickness(5, 3, 5, 3) });
}
