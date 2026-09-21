using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using XamlForge.Core;
using XamlForge.Designer;

namespace XamlForge.App;

public sealed class MainWindow : Window
{
    readonly Workspace workspace;
    readonly string settingsPath=AppSettings.UserSettingsPath;
    AppSettings settings;
    bool closePending,closeApproved,dialogOpen;

    public MainWindow()
    {
        Title="XamlForge";FontFamily=new FontFamily("Segoe UI");FontSize=12;
        Width=Math.Min(1440,SystemParameters.WorkArea.Width*.92);
        Height=Math.Min(940,SystemParameters.WorkArea.Height*.92);
        MinWidth=Math.Min(1000,SystemParameters.WorkArea.Width);MinHeight=Math.Min(600,SystemParameters.WorkArea.Height);WindowStartupLocation=WindowStartupLocation.CenterScreen;
        settings=AppSettings.Load(File.Exists(settingsPath)?settingsPath:Path.Combine(AppContext.BaseDirectory,"settings.json"));
        UiAppearance.Configure(settings,ResolveDark());
        workspace=new Workspace();Content=workspace;
        workspace.ConfirmSaveAsync=ConfirmSaveAsync;
        workspace.Commands.SettingsRequested+=ShowSettings;
        workspace.Commands.AboutRequested+=ShowAbout;
        workspace.Commands.ExitRequested+=Close;
        var icon=Path.Combine(AppContext.BaseDirectory,"Assets","XamlForge.ico");
        if(File.Exists(icon))Icon=BitmapFrame.Create(new Uri(icon));
        ApplySettings();
        SystemEvents.UserPreferenceChanged+=OnSystemPreferenceChanged;
        Closing+=(s,e)=>{
            if(closeApproved)return;
            e.Cancel=true;
            if(closePending||dialogOpen)return;
            closePending=true;
            Dispatcher.BeginInvoke(new Action(async()=>{
                try{if(await workspace.CanCloseAsync()){closeApproved=true;Close();}}
                finally{closePending=false;}
            }));
        };
        Closed+=(s,e)=>{SystemEvents.UserPreferenceChanged-=OnSystemPreferenceChanged;workspace.Dispose();};

    }

    bool ResolveDark()
    {
        if(settings.Theme!="System")return settings.Theme=="Dark";
        using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value&&value==0;
    }
    void OnSystemPreferenceChanged(object sender,UserPreferenceChangedEventArgs e)=>Dispatcher.BeginInvoke(new Action(ApplySettings));
    void ApplySettings()
    {
        UiAppearance.Configure(settings,ResolveDark());workspace.ApplyPreferences(settings,ResolveDark());
        Background=UiAppearance.Surface;Foreground=UiAppearance.Text;
    }

    Task<SaveDecision> ConfirmSaveAsync(string title)
    {
        var text=new TextBlock{Text=UiText.Get("Save changes to")+" “"+title+"”?",TextWrapping=TextWrapping.Wrap,MaxWidth=520};
        var dialog=new EditorDialog(this,"Save changes before closing?",text,("Save",1),("Don’t save",2),("Cancel",0));
        var result=ShowEditorDialog(dialog);
        return Task.FromResult(result switch{1=>SaveDecision.Save,2=>SaveDecision.Discard,_=>SaveDecision.Cancel});
    }
    void ShowSettings()
    {
        if(dialogOpen)return;
        var panel=new StackPanel{Width=400};
        var language=new ComboBox();foreach(var item in new[]{"System","English","Deutsch"})language.Items.Add(UiText.Get(item));
        language.SelectedIndex=Array.IndexOf(new[]{"System","en","de"},settings.Language);
        var theme=new ComboBox();foreach(var item in new[]{"System","Light","Dark"})theme.Items.Add(UiText.Get(item));
        theme.SelectedIndex=Array.IndexOf(new[]{"System","Light","Dark"},settings.Theme);
        var font=new ComboBox{IsEditable=false,IsTextSearchEnabled=true,MaxDropDownHeight=300};
        foreach(var item in Fonts.SystemFontFamilies.Select(f=>f.Source).Append(settings.EditorFont).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f=>f,StringComparer.CurrentCultureIgnoreCase))font.Items.Add(item);
        font.SelectedItem=font.Items.Cast<string>().First(f=>string.Equals(f,settings.EditorFont,StringComparison.OrdinalIgnoreCase));
        var size=new ComboBox{IsEditable=true};foreach(var item in new[]{"9","10","11","12","13","14","16","18","20","24","28"})size.Items.Add(item);size.Text=settings.EditorFontSize.ToString(System.Globalization.CultureInfo.CurrentCulture);
        void Field(string label,Control editor){panel.Children.Add(new TextBlock{Text=label,Margin=new Thickness(0,8,0,4)});editor.Padding=new Thickness(5,3,5,3);panel.Children.Add(editor);}
        Field("Language",language);Field("Theme",theme);Field("Editor font",font);Field("Font size",size);
        panel.Children.Add(new TextBlock{Text="Appearance changes do not alter your WPF design.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,4)});
        var dialog=new EditorDialog(this,"Settings",panel,("Save",1),("Cancel",0));
        if(ShowEditorDialog(dialog)!=1)return;
        if(!double.TryParse(size.Text,out var fontSize)||!double.IsFinite(fontSize))fontSize=settings.EditorFontSize;
        settings=new(new[]{"System","en","de"}[Math.Max(0,language.SelectedIndex)],
            new[]{"System","Light","Dark"}[Math.Max(0,theme.SelectedIndex)],
            font.SelectedItem as string ?? settings.EditorFont,Math.Clamp(fontSize,9,28));
        try{settings.Save(settingsPath);}catch(Exception e){workspace.Report(e.Message);}
        ApplySettings();
    }
    void ShowAbout()
    {
        if(dialogOpen)return;
        var panel=new StackPanel{Width=440};
        var brand=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,16)};
        brand.Children.Add(new Image{Width=42,Height=42,Source=new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory,"Assets","XamlForge.png")))});
        brand.Children.Add(new TextBlock{Text="XamlForge 26.09.0",FontSize=23,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0)});panel.Children.Add(brand);
        foreach(var text in new[]{"A visual WPF designer with integrated PowerShell editing.",UiText.Get("Developed for")+" @nicolux2006","Development build · compatibility verification in progress","Independent software. Not an official Microsoft product."})
            panel.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
        ShowEditorDialog(new EditorDialog(this,"About XamlForge",panel,("Close",1)));
    }
    int ShowEditorDialog(EditorDialog dialog)
    {
        if(dialogOpen)return 0;
        dialogOpen=true;
        try{dialog.ShowDialog();return dialog.SelectedAction;}
        finally{dialogOpen=false;}
    }
}
