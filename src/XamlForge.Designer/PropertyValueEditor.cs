using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace XamlForge.Designer;

// The document remains the authority: every editor submits invariant XAML through one transaction.
public sealed class PropertyValueEditor : DockPanel
{
    readonly Func<string?, bool> apply;
    readonly Type? type;
    string value;
    bool rebuilding;
    bool raw;
    readonly ContentControl host = new();
    public FrameworkElement Input => (FrameworkElement)host.Content;

    public PropertyValueEditor(string property, Type? propertyType, string value, Func<string?, bool> apply)
    {
        System.Windows.Automation.AutomationProperties.SetName(this, property);
        type = Nullable.GetUnderlyingType(propertyType ?? typeof(string)) ?? propertyType;
        this.value = value;
        this.apply = apply;
        raw = value.TrimStart().StartsWith('{');
        MinWidth = 125;
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var expression = new Button { Content = "fx", ToolTip = "Edit XAML value", MinWidth = 25, Padding = new Thickness(3) };
        expression.Click += (_, _) => { raw = !raw; Build(); UiAppearance.Apply(this); };
        var reset = new Button { Content = "↺", ToolTip = "Reset property", MinWidth = 25, Padding = new Thickness(3) };
        reset.Click += (_, _) => Submit(null);
        actions.Children.Add(expression); actions.Children.Add(reset);
        SetDock(actions, Dock.Right); Children.Add(actions); Children.Add(host);
        Build();
    }

    public bool Submit(string? next)
    {
        if (rebuilding) return false;
        if (next == value) return true;
        var accepted = apply(next);
        if (accepted) value = next ?? "";
        if (Input is Control control)
        {
            control.ToolTip = accepted ? null : XamlForge.Core.UiText.Get("Invalid property value");
            control.BorderBrush = accepted ? UiAppearance.Text : Brushes.IndianRed;
        }
        return accepted;
    }

    void Build()
    {
        rebuilding = true;
        try
        {
            FrameworkElement input;
            if (!raw && type == typeof(bool))
            {
                var check = new CheckBox { IsThreeState = true, IsChecked = bool.TryParse(value, out var flag) ? flag : null, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) };
                check.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CheckBox'><Border Width='18' Height='18' HorizontalAlignment='Left' BorderThickness='1' BorderBrush='{TemplateBinding Foreground}' Background='{TemplateBinding Background}'><TextBlock x:Name='Mark' Text='✓' Foreground='{TemplateBinding Foreground}' HorizontalAlignment='Center' VerticalAlignment='Center' Visibility='Hidden'/></Border><ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='Mark' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsChecked' Value='{x:Null}'><Setter TargetName='Mark' Property='Text' Value='−'/><Setter TargetName='Mark' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter Property='Background' Value='#407ACC'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");
                check.Click += (_, _) => Submit(check.IsChecked?.ToString());
                input = check;
            }
            else if (!raw && Choices() is { } choices)
            {
                var combo = new ComboBox { IsEditable = true, ItemsSource = choices, Text = value, MinHeight = 28, MaxDropDownHeight = 300, IsTextSearchEnabled = true };
                combo.SelectionChanged += (_, _) => { if (!rebuilding && combo.SelectedItem is string selected) Submit(selected); };
                combo.LostKeyboardFocus += (_, e) => { if (!combo.IsKeyboardFocusWithin) Submit(combo.Text); };
                combo.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Submit(combo.Text); e.Handled = true; } };
                input = combo;
            }
            else
            {
                var text = new TextBox { Text = value, MinHeight = 28, Padding = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center };
                text.LostKeyboardFocus += (_, _) => Submit(text.Text);
                text.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Submit(text.Text); e.Handled = true; } };
                if (!raw && IsNumber(type))
                {
                    var panel = new DockPanel();
                    var steps = new StackPanel();
                    foreach (var (label, delta) in new[] { ("▴", 1d), ("▾", -1d) })
                    {
                        var step = new Button { Content = label, FontSize = 9, Padding = new Thickness(4, 0, 4, 0), MinHeight = 14, Focusable = false };
                        step.Click += (_, _) => { if (double.TryParse(text.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)) { var next = (number + delta).ToString("G", CultureInfo.InvariantCulture); if (Submit(next)) text.Text = next; } };
                        steps.Children.Add(step);
                    }
                    SetDock(steps, Dock.Right); panel.Children.Add(steps); panel.Children.Add(text); input = panel;
                }
                else input = text;
            }
            if (!raw && (typeof(Brush).IsAssignableFrom(type ?? typeof(string)) || type == typeof(Color)))
            {
                var panel = new DockPanel();
                var swatch = new System.Windows.Shapes.Rectangle { Width = 16, Height = 16, Margin = new Thickness(3), Stroke = Brushes.Gray };
                try { swatch.Fill = (Brush)new BrushConverter().ConvertFromInvariantString(value)!; } catch { }
                SetDock(swatch, Dock.Left); panel.Children.Add(swatch); panel.Children.Add(input); input = panel;
            }
            System.Windows.Automation.AutomationProperties.SetName(input, System.Windows.Automation.AutomationProperties.GetName(this));
            host.Content = input;
        }
        finally { rebuilding = false; }
    }

    IEnumerable<string>? Choices()
    {
        if (type?.IsEnum == true) return Enum.GetNames(type);
        if (type == typeof(FontFamily)) return Fonts.SystemFontFamilies.Select(f => f.Source).OrderBy(n => n);
        if (type == typeof(Brush) || type == typeof(SolidColorBrush) || type == typeof(Color)) return typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name);
        var owner = type == typeof(FontWeight) ? typeof(FontWeights) : type == typeof(FontStyle) ? typeof(FontStyles) : type == typeof(FontStretch) ? typeof(FontStretches) : null;
        if (owner is not null) return owner.GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name);
        return null;
    }
    static bool IsNumber(Type? t) => t == typeof(double) || t == typeof(float) || t == typeof(decimal) || t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(short) || t == typeof(byte);
}
