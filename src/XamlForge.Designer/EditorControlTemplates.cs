using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace XamlForge.Designer;

internal static class EditorControlTemplates
{

    public static readonly ControlTemplate MenuItem = (ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="MenuItem">
          <Grid>
            <Border x:Name="Frame" Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}" BorderThickness="1" BorderBrush="Transparent">
              <Grid>
                <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
                <TextBlock x:Name="Check" Text="✓" Visibility="Collapsed" Margin="0,0,8,0" Foreground="{TemplateBinding Foreground}"/>
                <ContentPresenter Grid.Column="1" ContentSource="Header" RecognizesAccessKey="True" VerticalAlignment="Center" TextElement.Foreground="{TemplateBinding Foreground}"/>
                <TextBlock x:Name="Gesture" Grid.Column="2" Text="{TemplateBinding InputGestureText}" Margin="18,0,0,0" Foreground="{TemplateBinding Foreground}"/>
                <TextBlock x:Name="Arrow" Grid.Column="3" Text="›" Visibility="Collapsed" Margin="12,0,0,0" Foreground="{TemplateBinding Foreground}"/>
              </Grid>
            </Border>
            <Popup x:Name="PART_Popup" Placement="Right" PlacementTarget="{Binding ElementName=Frame}" IsOpen="{TemplateBinding IsSubmenuOpen}" AllowsTransparency="True" Focusable="False">
              <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" Padding="3" MinWidth="180">
                <ScrollViewer CanContentScroll="True" MaxHeight="600"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Cycle" KeyboardNavigation.TabNavigation="Cycle"/></ScrollViewer>
              </Border>
            </Popup>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="InputGestureText" Value=""><Setter TargetName="Gesture" Property="Visibility" Value="Collapsed"/></Trigger>
            <Trigger Property="Role" Value="TopLevelHeader"><Setter TargetName="PART_Popup" Property="Placement" Value="Bottom"/></Trigger>
            <Trigger Property="Role" Value="SubmenuHeader"><Setter TargetName="Arrow" Property="Visibility" Value="Visible"/></Trigger>
            <Trigger Property="IsChecked" Value="True"><Setter TargetName="Check" Property="Visibility" Value="Visible"/></Trigger>
            <Trigger Property="IsHighlighted" Value="True"><Setter TargetName="Frame" Property="Background" Value="{DynamicResource XamlForge.Hover}"/><Setter TargetName="Frame" Property="BorderBrush" Value="{DynamicResource XamlForge.SelectionBorder}"/></Trigger>
            <Trigger Property="IsSubmenuOpen" Value="True"><Setter TargetName="Frame" Property="Background" Value="{DynamicResource XamlForge.Hover}"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Frame" Property="Opacity" Value="0.5"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);

    public static ControlTemplate Item(string type) => (ControlTemplate)XamlReader.Parse($$"""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="{{type}}">
          <Border x:Name="Frame" Background="{TemplateBinding Background}" Padding="{TemplateBinding Padding}" BorderThickness="1" BorderBrush="Transparent">
            <ContentPresenter HorizontalAlignment="Stretch" VerticalAlignment="Center" TextElement.Foreground="{TemplateBinding Foreground}"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Frame" Property="Background" Value="{DynamicResource XamlForge.Hover}"/></Trigger>
            <Trigger Property="IsSelected" Value="True"><Setter TargetName="Frame" Property="Background" Value="{DynamicResource XamlForge.Selected}"/><Setter TargetName="Frame" Property="BorderBrush" Value="{DynamicResource XamlForge.SelectionBorder}"/></Trigger>
            <Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Frame" Property="BorderBrush" Value="{DynamicResource XamlForge.SelectionBorder}"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Frame" Property="Opacity" Value="0.5"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);
    public static readonly ControlTemplate ComboItem = Item("ComboBoxItem");
    public static readonly ControlTemplate ListItem = Item("ListBoxItem");

    public static readonly ControlTemplate TreeItem = (ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="TreeViewItem">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width="20"/><ColumnDefinition/></Grid.ColumnDefinitions>
            <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
            <ToggleButton x:Name="Expander" IsChecked="{Binding IsExpanded,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}" Focusable="False" Foreground="{TemplateBinding Foreground}">
              <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="Transparent"><TextBlock x:Name="Glyph" Text="›" Foreground="{TemplateBinding Foreground}" HorizontalAlignment="Center" VerticalAlignment="Center"/></Border><ControlTemplate.Triggers><Trigger Property="IsChecked" Value="True"><Setter TargetName="Glyph" Property="Text" Value="⌄"/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
            </ToggleButton>
            <Border x:Name="Frame" Grid.Column="1" Padding="5,4" Background="{TemplateBinding Background}" BorderThickness="1" BorderBrush="Transparent">
              <ContentPresenter x:Name="PART_Header" ContentSource="Header" TextElement.Foreground="{TemplateBinding Foreground}"/>
            </Border>
            <ItemsPresenter x:Name="ItemsHost" Grid.Row="1" Grid.Column="1"/>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger SourceName="Frame" Property="IsMouseOver" Value="True"><Setter TargetName="Frame" Property="Background" Value="{DynamicResource XamlForge.Hover}"/></Trigger>
            <Trigger Property="IsSelected" Value="True"><Setter TargetName="Frame" Property="Background" Value="{DynamicResource XamlForge.Selected}"/><Setter TargetName="Frame" Property="BorderBrush" Value="{DynamicResource XamlForge.SelectionBorder}"/></Trigger>
            <Trigger Property="IsExpanded" Value="False"><Setter TargetName="ItemsHost" Property="Visibility" Value="Collapsed"/></Trigger>
            <Trigger Property="HasItems" Value="False"><Setter TargetName="Expander" Property="Visibility" Value="Hidden"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);
    public static readonly ControlTemplate ComboBox = (ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ComboBox">
          <Grid SnapsToDevicePixels="True">
            <Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="26"/></Grid.ColumnDefinitions>
            <Border Grid.ColumnSpan="2" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1"/>
            <ToggleButton Grid.ColumnSpan="2" Focusable="False" IsChecked="{Binding IsDropDownOpen,RelativeSource={RelativeSource TemplatedParent},Mode=TwoWay}">
              <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border Background="Transparent"><TextBlock Text="⌄" Foreground="{Binding Foreground,RelativeSource={RelativeSource AncestorType=ComboBox}}" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,8,0"/></Border></ControlTemplate></ToggleButton.Template>
            </ToggleButton>
            <ContentPresenter x:Name="Selection" Margin="8,5,0,5" VerticalAlignment="Center" IsHitTestVisible="False" Content="{TemplateBinding SelectionBoxItem}" ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"/>
            <TextBox x:Name="PART_EditableTextBox" Visibility="Hidden" Margin="5,3,0,3" IsReadOnly="{TemplateBinding IsReadOnly}" Background="{TemplateBinding Background}" Foreground="{TemplateBinding Foreground}" BorderThickness="0">
              <TextBox.Template><ControlTemplate TargetType="TextBox"><ScrollViewer x:Name="PART_ContentHost"/></ControlTemplate></TextBox.Template>
            </TextBox>
            <Popup x:Name="PART_Popup" Grid.ColumnSpan="2" Placement="Bottom" IsOpen="{TemplateBinding IsDropDownOpen}" AllowsTransparency="True" Focusable="False">
              <Border MinWidth="{Binding ActualWidth,RelativeSource={RelativeSource TemplatedParent}}" MaxHeight="{TemplateBinding MaxDropDownHeight}" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="1" Padding="2">
                <ScrollViewer CanContentScroll="True"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer>
              </Border>
            </Popup>
          </Grid>
          <ControlTemplate.Triggers>
            <Trigger Property="IsEditable" Value="True"><Setter TargetName="Selection" Property="Visibility" Value="Hidden"/><Setter TargetName="PART_EditableTextBox" Property="Visibility" Value="Visible"/></Trigger>
            <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.5"/></Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
        """);
}
