using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace MajdataEdit_Neo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var _textEditor = this.FindControl<TextEditor>("Editor");
        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var _install = TextMate.InstallTextMate(_textEditor, _registryOptions);
        var registry = new Registry(_install.RegistryOptions);

        _install.SetGrammarFile("E:\\repos\\Tml-Simai\\simai\\syntaxes\\simai.tmLanguage.json");
    }
}