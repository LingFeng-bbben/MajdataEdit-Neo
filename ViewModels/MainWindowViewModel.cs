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

    public async Task SetFumenContent(string content)
    {
        if (CurrentSimaiFile is null) return;
        var text = CurrentSimaiFile.Fumens[SelectedDifficulty];
        if (text is null) return;
        CurrentSimaiFile.Fumens[SelectedDifficulty] = content;
        currentSimaiChart = await _simaiParser.ParseChartAsync(null, null, content);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    private int selectedDifficulty = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    private SimaiFile? currentSimaiFile = null;
    private SimaiChart? currentSimaiChart = null;

    [ObservableProperty]
    private TrackInfo? songTrackInfo = null;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTime))]
    private double trackTime = 0f;
    public string DisplayTime
    {
        get
        {
            var minute = (int)TrackTime / 60;
            double second = (int)(TrackTime - 60 * minute);
            return string.Format("{0}:{1:00}", minute, second);
        }
    }
    [ObservableProperty]
    private float trackZoomLevel = 4f;

    private SimaiParser _simaiParser = new SimaiParser();
    private string _maidataDir = "";
    private TrackReader _trackReader = new TrackReader();


    public void SlideZoomLevel(float delta)
    {
        var level = TrackZoomLevel + delta;
        if (level <= 0.1f) level = 0.1f;
        if (level > 10f) level = 10f;
        TrackZoomLevel = level;
    }

    public void SlideTrackTime(double delta)
    {
        var time = TrackTime - delta * 0.2 * TrackZoomLevel;
        if (time < 0) time = 0;
        else if (time > SongTrackInfo.Length) time = SongTrackInfo.Length;
        TrackTime = time;
    }

    public async Task OpenFile()
    {
        try
        {
            var file = await FileIOManager.DoOpenFilePickerAsync(FileIOManager.FileOpenerType.Maidata);
            if (file is null) return;
            var maidataPath = file.TryGetLocalPath();
            if (maidataPath is null) return;
            CurrentSimaiFile = await _simaiParser.ParseAsync(maidataPath);
            var fileInfo = new FileInfo(maidataPath);
            _maidataDir = fileInfo.Directory.FullName;
            SongTrackInfo = _trackReader.ReadTrack(_maidataDir);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    
}
