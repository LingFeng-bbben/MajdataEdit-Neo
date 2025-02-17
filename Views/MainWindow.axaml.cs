using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using MajdataEdit_Neo.Controls;
using MajdataEdit_Neo.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace MajdataEdit_Neo.Views;

public partial class MainWindow : Window
{
    MainWindowViewModel viewModel => (MainWindowViewModel)DataContext;
    TextEditor textEditor;
    SimaiVisualizerControl simaiVisual;
    public MainWindow()
    {
        InitializeComponent();
        //setup editor
        textEditor = this.FindControl<TextEditor>("Editor");
        textEditor.TextChanged += TextEditor_TextChanged;
        textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var _install = TextMate.InstallTextMate(textEditor, _registryOptions);
        var registry = new Registry(_install.RegistryOptions);
        _install.SetGrammarFile("simai.tmLanguage.json");
        //setup visualizer
        simaiVisual = this.FindControl<SimaiVisualizerControl>("SimaiVisual");
        simaiVisual.PointerWheelChanged += SimaiVisual_PointerWheelChanged;
        simaiVisual.PointerMoved += SimaiVisual_PointerMoved;
        //zoom buttons
        this.FindControl<Button>("ZoomIn").Click += ZoomIn_Click;
        this.FindControl<Button>("ZoomOut").Click += ZoomOut_Click;
    }

    private void Caret_PositionChanged(object? sender, System.EventArgs e)
    {
        //Debug.WriteLine("Je;");
        var seek = textEditor.SelectionStart;
        var location = textEditor.Document.GetLocation(seek);
        viewModel.SetCaretTime(new Point(location.Column, location.Line));
        //Debug.WriteLine($"{location.Line} {location.Column}");
    }

    static double? lastX = null;
    private void SimaiVisual_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as SimaiVisualizerControl);
        var x = point.Position.X;
        viewModel.IsPointerPressedSimaiVisual = point.Properties.IsLeftButtonPressed;
        if (lastX is null) lastX = x;
        var delta = x - lastX;
        if (point.Properties.IsLeftButtonPressed)
        {
            var docseek = viewModel.SlideTrackTime((float)delta*10f/Width);
            SeekToDocPos(docseek);
        }
        lastX = x;
    }

    private void ZoomIn_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.SlideZoomLevel(-0.3f);
    }
    private void ZoomOut_Click(object? sender, RoutedEventArgs e)
    {
        viewModel.SlideZoomLevel(0.3f);
    }

    private void SimaiVisual_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        var docseek = viewModel.SlideTrackTime(e.Delta.Y);
        SeekToDocPos(docseek);
    }

    private async void TextEditor_TextChanged(object? sender, System.EventArgs e)
    {
        //TODO: add timer
        await viewModel.SetFumenContent(((TextEditor)sender).Text);
    }

    private void SeekToDocPos(Point position)
    {
        var offset = textEditor.Document.GetOffset((int)position.Y+1, (int)position.X);
        textEditor.Select(offset, 0);
        textEditor.ScrollTo((int)position.Y + 1, (int)position.X);
        textEditor.Focus();
    }

}