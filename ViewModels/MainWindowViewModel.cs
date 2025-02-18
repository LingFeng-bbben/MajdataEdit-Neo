﻿using Avalonia;
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
using System.Collections.ObjectModel;
using MsBox.Avalonia;
using Avalonia.Win32.Interop.Automation;
using AvaloniaEdit;
using Avalonia.Data.Converters;

namespace MajdataEdit_Neo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        PropertyChanged += MainWindowViewModel_PropertyChanged;
    }

    private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        //Debug.WriteLine(e.PropertyName);
        if (e.PropertyName == nameof(CurrentSimaiFile))
        {
            //Debug.WriteLine("SimaiFileChanged");
            IsSaved = false;
        }
    }

    private string[] _level = new string[7];
    public string Level
    {
        get
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return "";
            _level[selectedDifficulty] = CurrentSimaiFile.Charts[SelectedDifficulty].Level;
            return _level[selectedDifficulty];
        }
        set
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return;
            CurrentSimaiFile.Charts[SelectedDifficulty].Level = value;
            Debug.WriteLine(SelectedDifficulty);
            SetProperty(ref _level[selectedDifficulty], value);
            OnPropertyChanged(nameof(CurrentSimaiFile));
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
            OnPropertyChanged(nameof(CurrentSimaiFile));
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
        OnPropertyChanged(nameof(CurrentSimaiFile));
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
    [NotifyPropertyChangedFor(nameof(IsLoaded))]
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
            OnPropertyChanged(nameof(CurrentSimaiFile));
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

    public bool IsLoaded
    {
        get
        {
            return CurrentSimaiFile is not null;
        }
    }

    private SimaiParser _simaiParser = new SimaiParser();
    private string _maidataDir = "";
    private TrackReader _trackReader = new TrackReader();

    public bool IsPointerPressedSimaiVisual { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor("WindowTitle")]
    private bool isSaved = true;
    public string WindowTitle
    {
        get
        {
            if (CurrentSimaiFile is null) return "MajdataEdit Neo";
            return "MajdataEdit Neo - " + CurrentSimaiFile.Title + (IsSaved ? "" : "*");
        }
    }

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

    public async Task NewFile()
    {
        if (await AskSave()) return;
        try
        {
            var file = await FileIOManager.DoOpenFilePickerAsync(FileIOManager.FileOpenerType.Track);
            if (file is null) return;
            var maidataPath = file.TryGetLocalPath();
            if (maidataPath is null) return;
            var fileInfo = new FileInfo(maidataPath);
            _maidataDir = fileInfo.Directory.FullName;
            if(File.Exists( _maidataDir+"/maidata.txt"))
            {
                var mainWindow = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                await MessageBoxManager.GetMessageBoxStandard(
                "Error", "Maidata Already Exist",
                MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error)
                .ShowWindowDialogAsync(mainWindow.MainWindow);
                return;
            }
            CurrentSimaiFile = SimaiFile.Empty("Set Title", "Set Artist");
            SongTrackInfo = _trackReader.ReadTrack(_maidataDir);
            IsSaved = false;
            OpenChartInfoWindow();
            //TODO: Trigger View Reload
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    public async Task OpenFile()
    {
        if (await AskSave()) return;
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
            IsSaved = true;
            
            //TODO: Trigger View Reload
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    //return: isCancel
    public async Task<bool> AskSave()
    {
        if (!IsSaved)
        {
            var mainWindow = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var result = await MessageBoxManager.GetMessageBoxStandard(
                "Warning", "Chart not yet saved.\nSave it now?", 
                MsBox.Avalonia.Enums.ButtonEnum.YesNoCancel, MsBox.Avalonia.Enums.Icon.Warning)
                .ShowWindowDialogAsync(mainWindow.MainWindow);
            if (result == MsBox.Avalonia.Enums.ButtonResult.Yes)
                SaveFile();
            if (result == MsBox.Avalonia.Enums.ButtonResult.No)
                ;//Do nothing and do not cancel
            else if (result == MsBox.Avalonia.Enums.ButtonResult.Cancel)
                return true;
            else if (result == MsBox.Avalonia.Enums.ButtonResult.None)
                return true;//user exit
        }
        return false;
    }

    public void SaveFile()
    {
        if (CurrentSimaiFile is null) return;
        IsSaved = true;
        _simaiParser.DeParse(CurrentSimaiFile, _maidataDir + "/maidata.txt");
    }

    public void OpenBpmTapWindow()
    {
        new BpmTapWindow().Show();
    }

    public async void OpenChartInfoWindow()
    {
        if (CurrentSimaiFile is null) return;
        var mainWindow = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (mainWindow is null || mainWindow.MainWindow is null) return;
        var window = new ChartInfoWindow();
        window.DataContext = new ChartInfoViewModel()
        {
            Title = CurrentSimaiFile.Title,
            Artist = CurrentSimaiFile.Artist,
            SimaiCommands = new ObservableCollection<SimaiCommand>(CurrentSimaiFile.Commands),
            MaidataDir = _maidataDir
        };
        await window.ShowDialog(mainWindow.MainWindow);
        var datacontext = window.DataContext as ChartInfoViewModel;
        if (datacontext is null) throw new Exception("Wtf");
        CurrentSimaiFile.Title = datacontext.Title;
        CurrentSimaiFile.Artist = datacontext.Artist;
        CurrentSimaiFile.Commands = datacontext.SimaiCommands.ToArray();
        await Task.Delay(100);
        OnPropertyChanged(nameof(CurrentSimaiFile));
        //TODO: Trigger View Reload
    }

    public void MirrorHorizontal(TextEditor editor)
    {
        editor.SelectedText = SimaiMirror.HandleMirror(editor.SelectedText, SimaiMirror.HandleType.LRMirror);
    }
    public void MirrorVertical(TextEditor editor)
    {
        editor.SelectedText = SimaiMirror.HandleMirror(editor.SelectedText, SimaiMirror.HandleType.UDMirror);
    }
    public void Mirror180(TextEditor editor)
    {
        editor.SelectedText = SimaiMirror.HandleMirror(editor.SelectedText, SimaiMirror.HandleType.HalfRotation);
    }
    public void Turn45(TextEditor editor)
    {
        editor.SelectedText = SimaiMirror.HandleMirror(editor.SelectedText, SimaiMirror.HandleType.Rotation45);
    }
    public void TurnNegative45(TextEditor editor)
    {
        Debug.WriteLine("Waht");
        editor.SelectedText = SimaiMirror.HandleMirror(editor.SelectedText, SimaiMirror.HandleType.CcwRotation45);
    }

}
