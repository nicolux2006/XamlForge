namespace XamlForge.Core;
public static class UiText
{
    public static string Language {get;set;}=new AppSettings().ResolveLanguage();
    public static string Get(string english)=>Language=="de"&&German.TryGetValue(english,out var translated)?translated:english;
    public static string Message(string message)
    {
        if(Language!="de")return message;
        if(German.TryGetValue(message,out var translated))return translated;
        foreach(var p in new[]{("Saved ","Gespeichert: "),("Copied ","Kopiert: "),("Type: ","Typ: ")})if(message.StartsWith(p.Item1))return p.Item2+message[p.Item1.Length..];
        return message;
    }
    static readonly Dictionary<string,string> German=new()
    {
        ["Read-only"]="Schreibgeschützt",["Complex value"]="Komplexer Wert",["Edit complex value in XAML"]="Komplexen Wert im XAML bearbeiten",
        ["Edit XAML value"]="XAML-Wert bearbeiten",["Reset property"]="Eigenschaft zurücksetzen",["Invalid property value"]="Ungültiger Eigenschaftswert",
        ["Minimize (design preview)"]="Minimieren (Entwurfsvorschau)",["Maximize (design preview)"]="Maximieren (Entwurfsvorschau)",["Close (design preview)"]="Schließen (Entwurfsvorschau)",
        ["File"]="Datei",["New"]="Neu",["＋ New"]="＋ Neu",["Open"]="Öffnen",["Open…"]="Öffnen…",["Save"]="Speichern",["Save As…"]="Speichern unter…",["Exit"]="Beenden",
        ["Edit"]="Bearbeiten",["Undo"]="Rückgängig",["Redo"]="Wiederholen",["Copy control"]="Element kopieren",["Paste control"]="Element einfügen",["Delete control"]="Element löschen",
        ["View"]="Ansicht",["Design"]="Entwurf",["Split"]="Geteilt",["Run"]="Ausführen",["▶ Run"]="▶ Ausführen",["Run / Preview   F5"]="Ausführen / Vorschau   F5",["Stop"]="Stoppen",["■ Stop"]="■ Stoppen",["Help"]="Hilfe",
        ["Settings"]="Einstellungen",["About XamlForge"]="Über XamlForge",["Language"]="Sprache",["Theme"]="Design",["System"]="System",["Light"]="Hell",["Dark"]="Dunkel",["Editor font"]="Editor-Schrift",["Font size"]="Schriftgröße",["Close"]="Schließen",["Cancel"]="Abbrechen",["Apply"]="Übernehmen",
        ["Toolbox"]="Werkzeugkasten",["All WPF controls"]="Alle WPF-Steuerelemente",["Filter WPF controls"]="WPF-Steuerelemente filtern",["Files"]="Dateien",["Outline"]="Hierarchie",["Properties"]="Eigenschaften",["ϟ Events"]="ϟ Ereignisse",["Filter properties"]="Eigenschaften filtern",
        ["PowerShell events"]="PowerShell-Ereignisse",["Apply handler"]="Ereigniscode übernehmen",["Apply XAML  Ctrl+Enter"]="XAML übernehmen  Strg+Enter",["Changes update the design after a short pause."]="Änderungen aktualisieren den Entwurf nach einer kurzen Pause.",
        ["Select an event in Properties"]="Ein Ereignis in den Eigenschaften auswählen",["Drag controls onto the page · Arrow keys move · Delete removes"]="Elemente hineinziehen · Pfeiltasten verschieben · Entf löscht",["Output / Error List"]="Ausgabe / Fehlerliste",["Ready"]="Bereit",["WPF DESIGNER"]="WPF-DESIGNER",
        ["Export to PowerShell"]="PowerShell-Export",["Export Design to PowerShell Script"]="Entwurf als PowerShell-Skript exportieren",["Close document"]="Dokument schließen",
        ["Don’t save"]="Nicht speichern",["Save changes to"]="Änderungen speichern in",["Save changes before closing?"]="Änderungen vor dem Schließen speichern?",["Select a control first."]="Zuerst ein Steuerelement auswählen.",
        ["Select a design document to export."]="Zum Exportieren ein Entwurfsdokument auswählen.",
        ["A preview is already running. Stop it first."]="Eine Vorschau läuft bereits. Zuerst stoppen.",["Running in Windows PowerShell (STA)"]="Wird in Windows PowerShell (STA) ausgeführt",["Preview finished"]="Vorschau beendet",["Stopped"]="Gestoppt",
        ["Use Save As → XamlForge project to preserve event handlers. Use Export for a standalone script."]="Mit ‚Speichern unter‘ als XamlForge-Projekt bleiben Ereignisse erhalten. Für ein eigenständiges Skript ‚Export‘ verwenden.",
        ["Enter a value or a WPF markup extension. Empty resets the property."]="Wert oder WPF-Markup-Erweiterung eingeben. Leer setzt die Eigenschaft zurück.",
        ["A visual WPF designer with integrated PowerShell editing."]="Visueller WPF-Designer mit integrierter PowerShell-Codebearbeitung.",
        ["Developed for"]="Entwickelt für",["Development build · compatibility verification in progress"]="Entwicklungsversion · Kompatibilitätsprüfung läuft",
        ["Independent software. Not an official Microsoft product."]="Unabhängige Software. Kein offizielles Microsoft-Produkt.",
        ["Appearance changes do not alter your WPF design."]="Darstellungsänderungen verändern deinen WPF-Entwurf nicht.",
        ["XamlForge project (includes events)|*.xfg|WPF XAML (layout only)|*.xaml"]="XamlForge-Projekt (mit Ereignissen)|*.xfg|WPF-XAML (nur Layout)|*.xaml",
        ["XamlForge, XAML or PowerShell|*.xfg;*.xaml;*.ps1|All files|*.*"]="XamlForge, XAML oder PowerShell|*.xfg;*.xaml;*.ps1|Alle Dateien|*.*",["PowerShell script|*.ps1"]="PowerShell-Skript|*.ps1"
    };
}
