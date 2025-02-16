using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using MajdataEdit_Neo.ViewModels;
using System.IO;
using System.Threading.Tasks;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace MajdataEdit_Neo.Views;

public partial class MainWindow : Window
{
    MainWindowViewModel viewModel => (MainWindowViewModel)DataContext;
    TextEditor textEditor;
    public MainWindow()
    {
        InitializeComponent();
        textEditor = this.FindControl<TextEditor>("Editor");
        textEditor.TextChanged += TextEditor_TextChanged;
        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var _install = TextMate.InstallTextMate(textEditor, _registryOptions);
        var registry = new Registry(_install.RegistryOptions);

        _install.SetGrammarFile("simai.tmLanguage.json");
    }

    private void TextEditor_TextChanged(object? sender, System.EventArgs e)
    {
        viewModel.SetFumenContent(((TextEditor)sender).Text);
    }
}