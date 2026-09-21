using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using XamlForge.Core;

namespace XamlForge.Designer;
public sealed class CodeEditor : TextEditor
{
    public Func<IEnumerable<string>>? Suggestions {get;set;}
    public CodeEditor(bool powershell=false)
    {
        ShowLineNumbers=true;FontFamily=new FontFamily("Cascadia Code, Consolas");FontSize=13;Background=Brushes.White;Foreground=Brushes.Black;
        Padding=new Thickness(8);HorizontalScrollBarVisibility=System.Windows.Controls.ScrollBarVisibility.Auto;VerticalScrollBarVisibility=System.Windows.Controls.ScrollBarVisibility.Auto;
        Options.ConvertTabsToSpaces=true;Options.IndentationSize=4;Options.HighlightCurrentLine=true;SearchPanel.Install(TextArea);
        if(!powershell)SyntaxHighlighting=HighlightingManager.Instance.GetDefinition("XML");
        else { using var reader=XmlReader.Create(new StringReader(PowerShellSyntax));SyntaxHighlighting=HighlightingLoader.Load(reader,HighlightingManager.Instance); }
        PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Space&&Keyboard.Modifiers==ModifierKeys.Control){Complete();e.Handled=true;}};
    }
    public void ApplyAppearance(AppSettings settings,bool dark)
    {
        FontFamily=new FontFamily(settings.EditorFont);FontSize=settings.EditorFontSize;
        Background=new SolidColorBrush(dark?Color.FromRgb(30,30,30):Colors.White);Foreground=new SolidColorBrush(dark?Color.FromRgb(220,220,220):Colors.Black);LineNumbersForeground=new SolidColorBrush(dark?Color.FromRgb(133,133,133):Color.FromRgb(100,110,120));
        foreach(var color in SyntaxHighlighting?.NamedHighlightingColors??[])
        {
            var name=color.Name??"";var hex=name.Contains("Comment",StringComparison.OrdinalIgnoreCase)?(dark?"#6A9955":"#008000"):
                name.Contains("String",StringComparison.OrdinalIgnoreCase)||name.Contains("AttributeValue",StringComparison.OrdinalIgnoreCase)?(dark?"#CE9178":"#A31515"):
                name.Contains("Variable",StringComparison.OrdinalIgnoreCase)?(dark?"#9CDCFE":"#AF00DB"):
                name.Contains("Attribute",StringComparison.OrdinalIgnoreCase)?(dark?"#9CDCFE":"#FF0000"):(dark?"#569CD6":"#0000FF");
            color.Foreground=new SimpleHighlightingBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        TextArea.TextView.Redraw();
    }
    void Complete()
    {
        var suggestions=Suggestions?.Invoke()?.Distinct().OrderBy(x=>x).ToArray();if(suggestions is not {Length:>0})return;
        var start=CaretOffset;while(start>0 && (char.IsLetterOrDigit(Text[start-1])||"$_-.".Contains(Text[start-1])))start--;
        var prefix=Text[start..CaretOffset];var window=new CompletionWindow(TextArea){StartOffset=start};
        foreach(var s in suggestions.Where(s=>s.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)))window.CompletionList.CompletionData.Add(new Completion(s));
        if(window.CompletionList.CompletionData.Count>0)window.Show();
    }
    sealed class Completion(string text) : ICompletionData
    {
        public ImageSource? Image=>null;public string Text=>text;public object Content=>text;public object Description=>"Insert "+text;public double Priority=>0;
        public void Complete(TextArea area,ISegment segment,EventArgs e)=>area.Document.Replace(segment,Text);
    }
    const string PowerShellSyntax="""
    <SyntaxDefinition name="PowerShell" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
      <Color name="Comment" foreground="#008000"/><Color name="String" foreground="#A31515"/><Color name="Keyword" foreground="#0000FF"/><Color name="Variable" foreground="#AF00DB"/>
      <RuleSet>
        <Span color="Comment" begin="&lt;\#" end="\#&gt;" multiline="true"/>
        <Span color="Comment" begin="\#"/>
        <Span color="String" begin="@'" end="^'@" multiline="true"/>
        <Span color="String" begin="'" end="'"/>
        <Span color="String" begin="&quot;" end="&quot;"/>
        <Rule color="Variable">\$[\w:]+</Rule>
        <Keywords color="Keyword"><Word>function</Word><Word>param</Word><Word>if</Word><Word>else</Word><Word>foreach</Word><Word>return</Word><Word>try</Word><Word>catch</Word><Word>finally</Word><Word>throw</Word><Word>switch</Word><Word>while</Word><Word>begin</Word><Word>process</Word><Word>end</Word></Keywords>
        <Rule color="Keyword">\b[A-Za-z]+-[A-Za-z]+\b</Rule>
      </RuleSet>
    </SyntaxDefinition>
    """;
}
