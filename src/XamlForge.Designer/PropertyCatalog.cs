using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace XamlForge.Designer;

public sealed record InspectableProperty(string Name, Type Type, bool ReadOnly, bool TextEditable, Func<object, object?> Read);

public static class PropertyCatalog
{

    static readonly HashSet<string> useful = new(StringComparer.Ordinal)
    {
        "Width","Height","MinWidth","MinHeight","MaxWidth","MaxHeight","Margin","Padding",
        "HorizontalAlignment","VerticalAlignment","HorizontalContentAlignment","VerticalContentAlignment",
        "Background","Foreground","BorderBrush","BorderThickness","CornerRadius","Opacity","Visibility",
        "FontFamily","FontSize","FontWeight","FontStyle","TextAlignment","TextWrapping","TextTrimming",
        "Content","Text","Header","Title","ToolTip","IsEnabled","IsReadOnly","IsChecked","IsThreeState",
        "IsDefault","IsCancel","IsExpanded","IsEditable","IsDropDownOpen","AcceptsReturn","AcceptsTab","MaxLength",
        "Orientation","FlowDirection","ItemWidth","ItemHeight","LastChildFill","RowDefinitions","ColumnDefinitions",
        "Items","ItemsSource","DisplayMemberPath","SelectedValuePath","SelectedIndex","SelectedItem","SelectedValue","SelectionMode",
        "Minimum","Maximum","Value","SmallChange","LargeChange","TickFrequency","IsSnapToTickEnabled","IsIndeterminate",
        "Source","Stretch","StretchDirection","UriSource","NavigateUri",
        "SelectedDate","DisplayDate","DisplayDateStart","DisplayDateEnd","FirstDayOfWeek","IsTodayHighlighted",
        "AutoGenerateColumns","Columns","CanUserAddRows","CanUserDeleteRows","CanUserSortColumns","GridLinesVisibility","HeadersVisibility",
        "HorizontalScrollBarVisibility","VerticalScrollBarVisibility","CanContentScroll",
        "ResizeMode","WindowStartupLocation","WindowState","Topmost","ShowInTaskbar","SizeToContent","WindowStyle",
        "PasswordChar","CaretBrush","SelectionBrush","TextDecorations"
    };
    public static bool IsUseful(InspectableProperty property, Type? parentType) => CanEdit(property) &&
        (useful.Contains(property.Name) ||
         parentType == typeof(System.Windows.Controls.Grid) && property.Name is "Grid.Row" or "Grid.Column" or "Grid.RowSpan" or "Grid.ColumnSpan" ||
         parentType == typeof(System.Windows.Controls.Canvas) && property.Name is "Canvas.Left" or "Canvas.Top" or "Canvas.Right" or "Canvas.Bottom" ||
         parentType == typeof(System.Windows.Controls.DockPanel) && property.Name == "DockPanel.Dock");
    public static bool CanEdit(InspectableProperty property) => !property.ReadOnly || typeof(System.Collections.IList).IsAssignableFrom(property.Type) || typeof(System.Collections.IDictionary).IsAssignableFrom(property.Type) || property.Type.GetInterfaces().Any(i=>i.IsGenericType && i.GetGenericTypeDefinition()==typeof(ICollection<>));
    static readonly Dictionary<Type,IReadOnlyList<InspectableProperty>> cache = new();
    public static IReadOnlyList<InspectableProperty> For(Type type)
    {
        if(cache.TryGetValue(type,out var cached))return cached;
        var result = new Dictionary<string, InspectableProperty>();
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(type))
            result[property.Name] = new(property.Name, property.PropertyType, property.IsReadOnly,
                property.Converter.CanConvertFrom(typeof(string)) || property.PropertyType == typeof(object), property.GetValue);
        // Include public CLR properties even if a custom designer descriptor hides them.
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod?.IsPublic == true))
            result.TryAdd(property.Name, new(property.Name, property.PropertyType, property.SetMethod?.IsPublic != true,
                TypeDescriptor.GetConverter(property.PropertyType).CanConvertFrom(typeof(string)) || property.PropertyType == typeof(object), property.GetValue));
        // Attached properties have no instance CLR property (Grid.Row, Canvas.Left, etc.).
        foreach (var owner in typeof(FrameworkElement).Assembly.GetExportedTypes().Concat(typeof(UIElement).Assembly.GetExportedTypes()).Distinct())
        foreach (var getter in owner.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => m.Name.StartsWith("Get", StringComparison.Ordinal) && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(type)))
        {
            var name = getter.Name[3..];
            if (owner.GetField(name + "Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null) is not DependencyProperty dp) continue;
            var setter = owner.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(m => m.Name == "Set" + name && m.GetParameters().Length == 2);
            var qualified = owner.Name + "." + name;
            result.TryAdd(qualified, new(qualified, dp.PropertyType, dp.ReadOnly || !setter,
                TypeDescriptor.GetConverter(dp.PropertyType).CanConvertFrom(typeof(string)) || dp.PropertyType == typeof(object), o => ((DependencyObject)o).GetValue(dp)));
        }
        return cache[type]=result.Values.OrderBy(p => p.Name).ToArray();
    }
}
