using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MajdataEdit_Neo.Views;
using MajdataEdit_Neo.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MajSimai;
using CommunityToolkit.Mvvm.ComponentModel;
using AvaloniaEdit.Document;


namespace MajdataEdit_Neo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    private int selectedDifficulty = 0;

    public string Level
    {
        get
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Levels[SelectedDifficulty] is null) return "";
            var text = CurrentSimaiFile.Levels[SelectedDifficulty].Level;
            if (text is null) return "";
            return text;
        }
        set
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Levels[SelectedDifficulty] is null) return;
            var text = CurrentSimaiFile.Levels[SelectedDifficulty].Level;
            if (text is null) return;
            SetProperty(ref text, value);
            CurrentSimaiFile.Levels[SelectedDifficulty].Level = text;
        }
    }

    public TextDocument FumenContent
    {
        get
        {
            if (CurrentSimaiFile is null) return new TextDocument();
            var text = CurrentSimaiFile.Fumens[SelectedDifficulty];
            if(text is null) return new TextDocument();
            return new TextDocument(CurrentSimaiFile.Fumens[SelectedDifficulty] as string);
        }
        //setter not working here, so using the event instead
    }

    public void SetFumenContent(string content)
    {
        if (CurrentSimaiFile is null) return;
        var text = CurrentSimaiFile.Fumens[SelectedDifficulty];
        if (text is null) return;
        CurrentSimaiFile.Fumens[SelectedDifficulty] = content;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    private SimaiFile? currentSimaiFile = null;

    private SimaiParser _simaiParser = new SimaiParser();
    private string _currentPath = "";

    public async Task OpenFile()
    {
        try
        {
            var file = await FileIOManager.DoOpenFilePickerAsync(FileIOManager.FileOpenerType.Maidata);
            if (file is null) return;
            _currentPath = file.TryGetLocalPath();
            if (_currentPath is null) return;
            CurrentSimaiFile = await _simaiParser.ParseAsync(_currentPath);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    
}
