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
using System.Linq;
using System.Timers;

namespace MajdataEdit_Neo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Level
    {
        get
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return "";
            var text = CurrentSimaiFile.Charts[SelectedDifficulty].Level;
            if (text is null) return "";
            return text;
        }
        set
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return;
            var text = CurrentSimaiFile.Charts[SelectedDifficulty].Level;
            if (text is null) return;
            SetProperty(ref text, value);
            CurrentSimaiFile.Charts[SelectedDifficulty].Level = text;
        }
    }

    public string Designer
    {
        get
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return "";
            var text = CurrentSimaiFile.Charts[SelectedDifficulty].Designer;
            if (text is null) return "";
            return text;
        }
        set
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return;
            var text = CurrentSimaiFile.Charts[SelectedDifficulty].Designer;
            if (text is null) return;
            SetProperty(ref text, value);
            CurrentSimaiFile.Charts[SelectedDifficulty].Designer = text;
        }
    }

    public TextDocument FumenContent
    {
        get
        {
            if (CurrentSimaiFile is null) return new TextDocument();
            var text = CurrentSimaiFile.RawCharts[SelectedDifficulty];
            if(text is null) return new TextDocument();
            return new TextDocument(CurrentSimaiFile.RawCharts[SelectedDifficulty] as string);
        }
        //setter not working here, so using the event instead
    }

    public async Task SetFumenContent(string content)
    {
        if (CurrentSimaiFile is null) return;
        var text = CurrentSimaiFile.RawCharts[SelectedDifficulty];
        if (text is null) CurrentSimaiFile.RawCharts[SelectedDifficulty] = "";
        CurrentSimaiFile.RawCharts[SelectedDifficulty] = content;
        try
        {
            CurrentSimaiChart = await _simaiParser.ParseChartAsync(null, null, content);
        }catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    [NotifyPropertyChangedFor(nameof(Designer))]
    private int selectedDifficulty = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    [NotifyPropertyChangedFor(nameof(Designer))]
    [NotifyPropertyChangedFor(nameof(Offset))]
    private SimaiFile? currentSimaiFile = null;
    [ObservableProperty]
    private SimaiChart? currentSimaiChart = null;

    private float _offset = 0;
    public float Offset
    {
        get
        {
            if (CurrentSimaiFile is null) return _offset;
            _offset = CurrentSimaiFile.Offset;
            return _offset;
        }
        set
        {
            if (CurrentSimaiFile is null) return;
            CurrentSimaiFile.Offset = value;
            SetProperty(ref _offset, value);
        }
    }

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
    private double caretTime = 0f;
    [ObservableProperty]
    private float trackZoomLevel = 4f;

    private SimaiParser _simaiParser = new SimaiParser();
    private string _maidataDir = "";
    private TrackReader _trackReader = new TrackReader();

    public bool IsPointerPressedSimaiVisual { get; set; }

    public void SlideZoomLevel(float delta)
    {
        var level = TrackZoomLevel + delta;
        if (level <= 0.1f) level = 0.1f;
        if (level > 10f) level = 10f;
        TrackZoomLevel = level;
    }

    //returns raw postion in chart
    public Point SlideTrackTime(double delta)
    {
        if (SongTrackInfo is null) return new Point();
        var time = TrackTime - delta * 0.2 * TrackZoomLevel;
        if (time < 0) time = 0;
        else if (time > SongTrackInfo.Length) time = SongTrackInfo.Length;
        TrackTime = time;
        if (CurrentSimaiChart is null) return new Point();
        var nearestNote = CurrentSimaiChart.CommaTimings.MinBy(o => Math.Abs(o.Timing + Offset - time));
        if (nearestNote is null) return new Point();
        return new Point(nearestNote.RawTextPositionX, nearestNote.RawTextPositionY);
    }

    public void SetCaretTime(Point rawPostion, bool setTrackTime)
    {
        if (CurrentSimaiChart is null) return;
        var nearestNote = CurrentSimaiChart.CommaTimings.Where(o => o.RawTextPositionY == rawPostion.Y-1).MinBy(o => Math.Abs(o.RawTextPositionX - rawPostion.X));
        if (nearestNote is null) return;
        CaretTime = nearestNote.Timing;
        if (setTrackTime) TrackTime = CaretTime+Offset;
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

    public void SaveFile()
    {
        if (CurrentSimaiFile is null) return;
        _simaiParser.DeParse(CurrentSimaiFile, _maidataDir + "/maidata1.txt");
    }

    public void OpenBpmTapWindow()
    {
        new BpmTapWindow().Show();
    }
    
}
