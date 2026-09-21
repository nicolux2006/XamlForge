using System.IO;
using System.Windows;
using XamlForge.Core;
namespace XamlForge.App;
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException+=(_,e)=>
        {
            try
            {
                Directory.CreateDirectory(AppSettings.UserDataDirectory);
                File.AppendAllText(Path.Combine(AppSettings.UserDataDirectory,"xamlforge-error.log"),e.Exception+Environment.NewLine);
            }
            catch(Exception error) when(error is IOException or UnauthorizedAccessException) { }
        };
    }
    protected override void OnStartup(StartupEventArgs e){base.OnStartup(e);MainWindow=new MainWindow();MainWindow.Show();}
}
