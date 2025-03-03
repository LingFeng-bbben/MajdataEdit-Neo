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
using System.Collections.ObjectModel;
using MsBox.Avalonia;
using AvaloniaEdit;
using MsBox.Avalonia.Enums;
using MajdataPlay.View.Types;

namespace MajdataEdit_Neo.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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
    public string DisplayTime
    {
        get
        {
            var minute = (int)TrackTime / 60;
            double second = (int)(TrackTime - 60 * minute);
            return string.Format("{0}:{1:00}", minute, second);
        }
    }
    public bool IsLoaded
    {
        get
        {
            return CurrentSimaiFile is not null;
        }
    }
    public bool IsPointerPressedSimaiVisual { get; set; }
    public string WindowTitle
    {
        get
        {
            if (CurrentSimaiFile is null) return "MajdataEdit Neo";
            return "MajdataEdit Neo - " + CurrentSimaiFile.Title + (IsSaved ? "" : "*");
        }
    }
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
            if (text is null) return new TextDocument();
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    [NotifyPropertyChangedFor(nameof(Designer))]
    int selectedDifficulty = 0;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenContent))]
    [NotifyPropertyChangedFor(nameof(Level))]
    [NotifyPropertyChangedFor(nameof(Designer))]
    [NotifyPropertyChangedFor(nameof(Offset))]
    [NotifyPropertyChangedFor(nameof(IsLoaded))]
    SimaiFile? currentSimaiFile = null;
    [ObservableProperty]
    SimaiChart? currentSimaiChart = null;
    [ObservableProperty]
    double caretTime = 0f;
    [ObservableProperty]
    float trackZoomLevel = 4f;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    bool isSaved = true;
    [ObservableProperty]
    TrackInfo? songTrackInfo = null;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTime))]
    double trackTime = 0f;


    float _offset = 0;
    readonly string[] _level = new string[7];
    PlayerConnection _playerConnection = new PlayerConnection();

    SimaiParser _simaiParser = new SimaiParser();
    TrackReader _trackReader = new TrackReader();
    public MainWindowViewModel()
    {
        PropertyChanged += MainWindowViewModel_PropertyChanged;
        _playerConnection.OnPlayStarted += _playerConnection_OnPlayStarted;
    }

    public async Task<bool> ConnectToPlayerAsync()
    {
        if (!await _playerConnection.ConnectAsync())
        {
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var msgBox = MessageBoxManager.GetMessageBoxStandard(
                    title: "Warning",
                    text: "Cannot connect to player",
                    @enum: ButtonEnum.Ok,
                    icon: Icon.Warning);
            if (mainWindow is null)
            {
                await msgBox.ShowWindowAsync();
            }
            else
            {
                await msgBox.ShowWindowDialogAsync(mainWindow);
            }
            return false;
        }
        return true;
    }

    private string _maidataDir = "";

    [ObservableProperty]
    private bool isFollowCursor;

    public void SlideZoomLevel(float delta)
    {
        var level = TrackZoomLevel + delta;
        if (level <= 0.1f) level = 0.1f;
        if (level > 10f) level = 10f;
        TrackZoomLevel = level;
    }

    /// <summary>
    /// returns raw postion in chart
    /// </summary>
    /// <param name="delta"></param>
    /// <returns></returns>
    public Point SlideTrackTime(double delta)
    {
        if (SongTrackInfo is null) return new Point();
        var time = TrackTime - delta * 0.2 * TrackZoomLevel;
        if (time < 0) time = 0;
        else if (time > SongTrackInfo.Length) time = SongTrackInfo.Length;
        Stop(false);
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
        if (setTrackTime) {
            Stop(false);
            TrackTime = CaretTime + Offset;
        }
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
            //TODO: Reset view if already loaded?
            var useOgg = File.Exists(_maidataDir + "/track.ogg");
            var trackPath = _maidataDir + "/track" + (useOgg ? ".ogg" : ".mp3");

            var bgPath = _maidataDir + "/bg.jpg";
            if (!File.Exists(bgPath)) bgPath = _maidataDir + "/bg.png";
            else if (!File.Exists(bgPath)) bgPath = "";

            var pvPath = _maidataDir + "/pv.mp4";
            if (!File.Exists(pvPath)) pvPath = _maidataDir + "/bg.mp4";
            else if (!File.Exists(pvPath)) pvPath = "";

            await _playerConnection.LoadAsync(trackPath, bgPath, pvPath);
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
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var msgBox = MessageBoxManager.GetMessageBoxStandard(
                title: "Warning", 
                text : "Chart not yet saved.\nSave it now?", 
                @enum: ButtonEnum.YesNoCancel, 
                icon : Icon.Warning);
            ButtonResult result;
            if (mainWindow is null)
            {
                result = await msgBox.ShowWindowAsync();
            }
            else
            {
                result = await msgBox.ShowWindowDialogAsync(mainWindow);
            }
            
            switch (result)
            {
                case ButtonResult.Yes:
                    SaveFile();
                    return false;
                case ButtonResult.No:
                    return false;
                default:
                    return true;

            }
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
        editor.SelectedText = SimaiMirror.HandleMirror(editor.SelectedText, SimaiMirror.HandleType.CcwRotation45);
    }

    private double playStartTime = 0d;
    public async void PlayPause(TextEditor textEditor)
    {
        if(!_playerConnection.IsConnected)
        {
            if (!await _playerConnection.ConnectAsync())
            {
                var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
                var msgBox = MessageBoxManager.GetMessageBoxStandard(
                        title: "Warning",
                        text: "Cannot connect to player",
                        @enum: ButtonEnum.Ok,
                        icon: Icon.Warning);
                if (mainWindow is null)
                {
                    await msgBox.ShowWindowAsync();
                }
                else
                {
                    await msgBox.ShowWindowDialogAsync(mainWindow);
                }
                return;
            }
        }
        switch(_playerConnection.ViewSummary.State)
        {
            case ViewStatus.Playing:
                await _playerConnection.PauseAsync();
                return;
            case ViewStatus.Paused:
                await _playerConnection.ResumeAsync();
                playStartTime = TrackTime;
                return;
        }

        playStartTime = TrackTime;
        await _playerConnection.ParseAndPlayAsync(playStartTime, Offset, CurrentSimaiFile.RawCharts[SelectedDifficulty], 1);
    }

    private async void _playerConnection_OnPlayStarted(object sender, MajdataPlay.View.Types.MajWsResponseType e)
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        while (_playerConnection.ViewSummary.State == MajdataPlay.View.Types.ViewStatus.Playing)
        {
            TrackTime = watch.ElapsedMilliseconds / 1000d + playStartTime;
            /*if (IsFollowCursor)
            {
                var nearestNote = CurrentSimaiChart.CommaTimings.MinBy(o => Math.Abs(o.Timing + Offset - TrackTime));
                if (nearestNote is null) continue;

                var point = new Point(nearestNote.RawTextPositionX, nearestNote.RawTextPositionY);
                SeekToDocPos(point, textEditor);
            }*/
            await Task.Delay(16);
        }
    }

    public async void Stop(bool isBackToStart = true)
    {
        if (isBackToStart)
            TrackTime = playStartTime;
        await _playerConnection.StopAsync();

    }

    public void SeekToDocPos(Point position, TextEditor editor)
    {
        var offset = editor.Document.GetOffset((int)position.Y + 1, (int)position.X);
        editor.Select(offset, 0);
        editor.ScrollTo((int)position.Y + 1, (int)position.X);
        editor.Focus();
    }

    private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        //Debug.WriteLine(e.PropertyName);
        if (e.PropertyName == nameof(CurrentSimaiFile))
        {
            Debug.WriteLine("SimaiFileChanged");
            IsSaved = false;
        }
    }
}
