using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using MajdataEdit_Neo.Controls;
using MajdataEdit_Neo.Models;
using MajdataEdit_Neo.ViewModels;
using System;
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
        textEditor.Options.HighlightCurrentLine = true;
        textEditor.Options.EnableTextDragDrop = true;
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
        //this window
        this.KeyDown += MainWindow_KeyDown;
        this.KeyUp += MainWindow_KeyUp;
        this.LostFocus += MainWindow_LostFocus;
        this.Closing += MainWindow_Closing;
        this.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        await viewModel.ConnectToPlayerAsync();
    }

    bool haveAsked = false;
    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (haveAsked) return;
        e.Cancel = true;
        haveAsked = true;
        if (!await viewModel.AskSave()) this.Close();
        else haveAsked = false;
    }

    private void MainWindow_LostFocus(object? sender, RoutedEventArgs e)
    {
        isCtrlKeyDown = false;
    }

    bool isCtrlKeyDown = false;

    private void MainWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        isCtrlKeyDown = false;
    }

    private void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        isCtrlKeyDown = e.Key == Avalonia.Input.Key.LeftCtrl;
    }

    private void Caret_PositionChanged(object? sender, System.EventArgs e)
    {
        var seek = textEditor.SelectionStart;
        var location = textEditor.Document.GetLocation(seek);
        viewModel.SetCaretTime(new Point(location.Column, location.Line), isCtrlKeyDown);
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
        if (isCtrlKeyDown)
        {
            viewModel.SlideZoomLevel(-0.3f * (float)e.Delta.Y);
        }
        else
        {
            var docseek = viewModel.SlideTrackTime(e.Delta.Y);
            SeekToDocPos(docseek);
        }
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

    private async void FindReplace_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (textEditor.SearchPanel.IsOpened)
            textEditor.SearchPanel.Close();
        else
        {
            textEditor.TextArea.Focus();
            await Task.Delay(100); // focus will cost time, or the searchpanel buttons wont work.
            textEditor.SearchPanel.Open();
        }
    }

    
}