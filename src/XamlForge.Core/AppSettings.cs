namespace XamlForge.Core;
public sealed record AppSettings(string Language="System",string Theme="System",string EditorFont="Cascadia Code",double EditorFontSize=13)
{
    public static string UserDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"XamlForge");
    public static string UserSettingsPath => Path.Combine(UserDataDirectory,"settings.json");
    public string ResolveLanguage(System.Globalization.CultureInfo? culture=null)=>Language is "de" or "en"?Language:(culture??System.Globalization.CultureInfo.CurrentUICulture).TwoLetterISOLanguageName=="de"?"de":"en";
    public static AppSettings Load(string path)
    {
        try {var s=System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path))??new();return s with{Language=s.Language is "de" or "en" or "System"?s.Language:"System",Theme=s.Theme is "Light" or "Dark" or "System"?s.Theme:"System",EditorFont=string.IsNullOrWhiteSpace(s.EditorFont)?"Cascadia Code":s.EditorFont,EditorFontSize=double.IsFinite(s.EditorFontSize)?Math.Clamp(s.EditorFontSize,9,28):13};}
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException){return new();}
    }
    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);var temporary=path+".tmp";File.WriteAllText(temporary,System.Text.Json.JsonSerializer.Serialize(this,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));File.Move(temporary,path,true);
    }
}
