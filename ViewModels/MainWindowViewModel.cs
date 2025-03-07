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
using MajdataEdit_Neo.Utils;
using Avalonia.Threading;
using MajdataEdit_Neo.Modules.AutoSave;
using MajdataEdit_Neo.Modules.AutoSave.Contexts;
using System.Runtime.InteropServices;
using MajdataEdit_Neo.Types;
using DiscordRPC;

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
    public bool IsFumenContextChanged
    {
        get
        {
            _autoSaveManager.IsFileChanged = !IsSaved;
            return _autoSaveManager.IsFileChanged;
        }
        set
        {
            IsSaved = !value;
            _autoSaveManager.IsFileChanged = value;
        }
    }
    public string Level
    {
        get
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return "";
            _level[SelectedDifficulty] = CurrentSimaiFile.Charts[SelectedDifficulty].Level;
            return _level[SelectedDifficulty];
        }
        set
        {
            if (CurrentSimaiFile is null || CurrentSimaiFile.Charts[SelectedDifficulty] is null) return;
            CurrentSimaiFile.Charts[SelectedDifficulty].Level = value;
            Debug.WriteLine(SelectedDifficulty);
            SetProperty(ref _level[SelectedDifficulty], value);
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
    public TextDocument FumenDocument
    {
        get
        {
            if (CurrentSimaiFile is null) return new TextDocument();
            var text = CurrentSimaiFile.RawCharts[SelectedDifficulty];
            if (text is null) return new TextDocument();
            ref var fumenContent = ref CurrentSimaiFile.RawCharts[SelectedDifficulty];
            OriginFumen = fumenContent;
            return new TextDocument(fumenContent);
        }
        //setter not working here, so using the event instead
    }
    public string CurrentFumen 
    {
        get
        {
            if (CurrentSimaiFile is null)
                return string.Empty;

            return CurrentSimaiFile.RawCharts[SelectedDifficulty];
        }
    }
    public string OriginFumen { get; set; } = string.Empty;

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
            //IsSaved = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public bool IsConnected
    {
        get
        {
            return _playerConnection.IsConnected;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenDocument))]
    [NotifyPropertyChangedFor(nameof(Level))]
    [NotifyPropertyChangedFor(nameof(Designer))]
    int selectedDifficulty = 0;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FumenDocument))]
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
    [ObservableProperty]
    private bool isFollowCursor;
    [ObservableProperty]
    private bool isPlayControlEnabled = true;
    [ObservableProperty]
    private bool isAnimated = true;

    bool _isBackToStartOnPlayStop = false;
    bool _isUpdatingAutoSaveContext = false;
    
    float _offset = 0;
    double playStartTime = 0d;
    DateTime _lastUpdateAutoSaveContextTime = DateTime.UnixEpoch;

    string _maidataDir = "";

    readonly string[] _level = new string[7];
    readonly Lock _syncLock = new();
    readonly DiscordRpcClient _dcRPCClient = new("1068882546932326481");
    readonly RichPresence _dcRichPresence = new()
    {
        Details = "Nothing to do",
        State = "",
        Assets = new Assets
        {
            LargeImageKey = "salt",
            LargeImageText = "Majdata",
            SmallImageKey = "None"
        }
    };
    readonly Lock _fumenContentChangedSyncLock = new();

    TextEditor _textEditor;

    PlayerConnection _playerConnection = new PlayerConnection();
    SimaiParser _simaiParser = new SimaiParser();
    TrackReader _trackReader = new TrackReader();
    InternalAutoSaveContext _internalLocalAutoSaveContext = new();
    InternalAutoSaveContext _internalGlobalAutoSaveContext = new();
    InternalAutoSaveContentProvider _internalAutoSaveContentProvider = new();
    AutoSaveManager _autoSaveManager;
    IAutoSaveRecoverer _autoSaveRecoverer;

    const int AUTOSAVE_CONTEXT_UPDATE_INTERVAL_MS = 5000;
    public MainWindowViewModel()
    {
        PropertyChanged += MainWindowViewModel_PropertyChanged;
        _playerConnection.OnPlayStarted += _playerConnection_OnPlayStarted;
        _playerConnection.OnPlayStopped += _playerConnection_OnPlayStopped;
        _playerConnection.OnLoadRequired += _playerConnection_OnLoadRequired;
        _playerConnection.OnLoadFinished += _playerConnection_OnLoadFinished;
        _playerConnection.OnDisconnected += _playerConnection_OnDisconnected;
        _internalLocalAutoSaveContext = new(_internalAutoSaveContentProvider);
        _internalGlobalAutoSaveContext = new InternalAutoSaveContext(_internalAutoSaveContentProvider);
        AutoSaveManager.Initialize(_internalLocalAutoSaveContext, _internalGlobalAutoSaveContext);
        _autoSaveManager = AutoSaveManager.Instance;
        _autoSaveRecoverer = _autoSaveManager.Recoverer;

        _autoSaveManager.OnAutoSaveExecuted += OnAutoSaveExecuted;
        _dcRPCClient.SetPresence(_dcRichPresence);
    }

    public async Task<bool> ConnectToPlayerAsync()
    {
        if (!await _playerConnection.ConnectAsync())
        {
            OnPropertyChanged(nameof(IsConnected));
            return false;
        }
        OnPropertyChanged(nameof(IsConnected));
        return true;
    }
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
        if(_playerConnection.IsConnected)
        {
            Stop(false);
        }
        TrackTime = time;
        if (CurrentSimaiChart is null) return new Point();
        var nearestNote = CurrentSimaiChart.CommaTimings.Where(o=> o.Timing + Offset - time < 0).MinBy(o => Math.Abs(o.Timing + Offset - time));
        if (nearestNote is null) return new Point();
        return new Point(nearestNote.RawTextPositionX, nearestNote.RawTextPositionY);
    }
    public async void SetCaretTime(int rawPostion, bool setTrackTime)
    {
        if (CurrentSimaiChart is null) return;
        var timings = CurrentSimaiChart.CommaTimings.ToArray();
        var nearestNote = timings.FirstOrDefault();
        //theLine = theLine.OrderBy(o => o.RawTextPositionX).ToArray();
        if (timings.Length >= 2)
        {
            for (int i = 0; i + 1 < timings.Length; i++)
            {
                var note = timings[i];
                var nextnote = timings[i + 1];
                if(rawPostion <= note.RawTextPosition)
                {
                    nearestNote = note;
                    break;
                }
                if (note.RawTextPosition < rawPostion && rawPostion <= nextnote.RawTextPosition)
                {
                    nearestNote = nextnote;
                    break;
                }
            }
        }
        if (nearestNote is null) return;
        CaretTime = nearestNote.Timing;
        if (IsFollowCursor|| setTrackTime) {
            //By pass Ctrl+Click if it's playing
            if (_playerConnection.ViewSummary.State == ViewStatus.Playing) return;
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
            //IsFumenContextChanged = false;
            _autoSaveManager.Enabled = true;
            _internalAutoSaveContentProvider.Content = await File.ReadAllTextAsync(maidataPath);
            UpdateAutoSaveContext();
            //TODO: Reset view if already loaded?
            await EditorLoad();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    private async Task EditorLoad()
    {
        try
        {
            IsPlayControlEnabled = false;
            var useOgg = File.Exists(_maidataDir + "/track.ogg");
            var trackPath = _maidataDir + "/track" + (useOgg ? ".ogg" : ".mp3");

            var bgPath = _maidataDir + "/bg.jpg";
            if (!File.Exists(bgPath)) bgPath = _maidataDir + "/bg.png";
            if (!File.Exists(bgPath)) bgPath = "";

            var pvPath = _maidataDir + "/pv.mp4";
            if (!File.Exists(pvPath)) pvPath = _maidataDir + "/bg.mp4";
            if (!File.Exists(pvPath)) pvPath = "";

            if (!await CheckPlayerConnectionAndReconnect())
            {
                return;
            }
            await _playerConnection.LoadAsync(trackPath, bgPath, pvPath);
        }
        catch
        {
        }
    }
    private void _playerConnection_OnLoadFinished(object? sender, EventArgs e)
    {
        IsPlayControlEnabled = true;
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
    public async void SaveFile()
    {
        if (CurrentSimaiFile is null) 
            return;
        lock(_fumenContentChangedSyncLock)
        {
            IsFumenContextChanged = false;
            OriginFumen = CurrentFumen;
        }
        await _simaiParser.DeParseAsync(CurrentSimaiFile, _maidataDir + "/maidata.txt");
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
        await EditorLoad();
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

    
    public async void PlayPause(TextEditor textEditor)
    {
        bool shouldRecoverPlayControl = true;
        try
        {
            IsPlayControlEnabled = false;
            if (!await CheckPlayerConnectionAndReconnect(true))
            {
                return;
            }
            switch (_playerConnection.ViewSummary.State)
            {
                case ViewStatus.Playing:
                    await _playerConnection.PauseAsync();
                    return;
                case ViewStatus.Paused:
                    await _playerConnection.ResumeAsync();
                    playStartTime = TrackTime;
                    return;
            }
            shouldRecoverPlayControl = false;
            playStartTime = TrackTime;
            _textEditor = textEditor;
            await _playerConnection.ParseAndPlayAsync(TrackTime, Offset, CurrentSimaiFile.RawCharts[SelectedDifficulty], 1);
        }
        finally
        {
            if (shouldRecoverPlayControl)
                IsPlayControlEnabled = true;
        }
    }

    public async void PlayStop(TextEditor textEditor)
    {
        bool shouldRecoverPlayControl = true;
        try
        {
            IsPlayControlEnabled = false;
            if (!await CheckPlayerConnectionAndReconnect(true))
            {
                TrackTime = playStartTime;
                return;
            }
            switch (_playerConnection.ViewSummary.State)
            {
                case ViewStatus.Playing:
                    _isBackToStartOnPlayStop = true;
                    await _playerConnection.StopAsync();
                    return;
                case ViewStatus.Paused:
                    await _playerConnection.ResumeAsync();
                    playStartTime = TrackTime;
                    return;
            }
            shouldRecoverPlayControl = false;
            playStartTime = TrackTime;
            _textEditor = textEditor;
            await _playerConnection.ParseAndPlayAsync(TrackTime, Offset, CurrentSimaiFile.RawCharts[SelectedDifficulty], 1);
        }
        finally
        {
            if (shouldRecoverPlayControl)
                IsPlayControlEnabled = true;
        }
    }
    
    private async void _playerConnection_OnPlayStarted(object sender, MajWsResponseType e)
    {
        IsPlayControlEnabled = true;
        await Task.Run(async () =>
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var timeA = watch.Elapsed;
            IsAnimated = false;
            while (_playerConnection.ViewSummary.State == ViewStatus.Playing && 
                    _playerConnection.IsConnected)
            {
                TrackTime = watch.ElapsedMilliseconds / 1000d + playStartTime;
                if (IsFollowCursor)
                {
                    var nearestNote = CurrentSimaiChart.CommaTimings.MinBy(o => Math.Abs(o.Timing + Offset - TrackTime));
                    if (nearestNote is null) continue;

                    var point = new Point(nearestNote.RawTextPositionX, nearestNote.RawTextPositionY);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SeekToDocPos(point, _textEditor);
                    });

                }
                var timeB = watch.Elapsed;
                var waitTime = Math.Max(16 - (int)(timeB - timeA).TotalMilliseconds, 0);
                await Task.Delay(waitTime);
            }
            IsAnimated = true;
        });
    }
    public async void Stop(bool isBackToStart = true)
    {
        _isBackToStartOnPlayStop = isBackToStart;
        try
        {
            IsPlayControlEnabled = false;
            if (!await CheckPlayerConnectionAndReconnect())
            {
                if (isBackToStart)
                    TrackTime = playStartTime;
                return;
            }
            switch (_playerConnection.ViewSummary.State)
            {
                case ViewStatus.Loaded:
                    if (isBackToStart)
                        break;
                    else
                        return;
                case ViewStatus.Ready:
                case ViewStatus.Playing:
                case ViewStatus.Paused:
                    break;
                default:
                    return;
            }
            await _playerConnection.StopAsync();
            
        }
        finally
        {
            IsPlayControlEnabled = true;
        }
        
    }

    private async void _playerConnection_OnPlayStopped(object sender, MajWsResponseType e)
    {
        await Task.Delay(32); // Wait the OnPlayStarted Loop to end
        if (_isBackToStartOnPlayStop)
            TrackTime = playStartTime;
        IsPlayControlEnabled = true;
    }

    private async void _playerConnection_OnLoadRequired(object? sender, EventArgs e)
    {
        await EditorLoad();
    }
    private void _playerConnection_OnDisconnected(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsConnected));
    }

    async Task<bool> CheckPlayerConnectionAndReconnect(bool showMessageBox = false)
    {
        //TODO: 改成弱提示，比如状态指示灯
        
        if (!_playerConnection.IsConnected)
        {
            if (!await _playerConnection.ConnectAsync())
            {
                OnPropertyChanged(nameof(IsConnected));
                return false;
            }
            OnPropertyChanged(nameof(IsConnected));
            return false;
        }
        OnPropertyChanged(nameof(IsConnected));
        return true;
    }
    public void SeekToDocPos(Point position, TextEditor editor)
    {
        var offset = editor.Document.GetOffset((int)position.Y + 1, (int)position.X);
        editor.Select(offset, 0);
        editor.ScrollTo((int)position.Y + 1, (int)position.X);
        editor.Focus();
    }
    void UpdateAutoSaveContext()
    {
        _internalLocalAutoSaveContext.RawFilePath = Path.Combine(_maidataDir, "maidata.txt");
        _internalLocalAutoSaveContext.WorkingPath = Path.Combine(_maidataDir, ".autosave");
        _internalGlobalAutoSaveContext.RawFilePath = Path.Combine(_maidataDir, "maidata.txt");
    }
    private async void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        //Debug.WriteLine(e.PropertyName);
        if (e.PropertyName == nameof(CurrentSimaiFile))
        {
            Debug.WriteLine("SimaiFileChanged");
            Stop(false);
            lock(_fumenContentChangedSyncLock)
            {
                if(OriginFumen == CurrentFumen)
                    IsFumenContextChanged = false;
                else
                    IsFumenContextChanged = true;
            }
            lock (_syncLock)
            {
                if ((DateTime.Now - _lastUpdateAutoSaveContextTime).TotalMilliseconds < AUTOSAVE_CONTEXT_UPDATE_INTERVAL_MS)
                    return;
                else if (_isUpdatingAutoSaveContext)
                    return;
                _isUpdatingAutoSaveContext = true;
                _lastUpdateAutoSaveContextTime = DateTime.Now;
            }

            try
            {
                if (CurrentSimaiFile is null)
                    return;
                var maidata = await SimaiParser.Shared.DeParseAsStringAsync(CurrentSimaiFile);
                _internalAutoSaveContentProvider.Content = maidata;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _isUpdatingAutoSaveContext = false;
            }

        }
    }
    void OnAutoSaveExecuted(object? sender)
    {
        if (!_fumenContentChangedSyncLock.TryEnter())
            return;
        try
        {
            IsFumenContextChanged = false;
            OriginFumen = CurrentFumen;
        }
        finally
        {
            _fumenContentChangedSyncLock.Exit();
        }
    }
    public void AboutButtonClicked(int index)
    {
        switch (index)
        {
            case 0:
                OpenBrowser("https://discord.gg/AcWgZN7j6K");
                break;
            case 1:
                OpenBrowser("https://qm.qq.com/q/GAxbFZHP6A");
                break;
            case 2:
                OpenBrowser("https://github.com/LingFeng-bbben/MajdataEdit-Neo");
                break;
            case 3:
                OpenBrowser("https://majdata.net/");
                break;
        }
    }

    private void OpenBrowser(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); // Works ok on windows
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);  // Works ok on linux
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url); // Not tested
        }
    }
    class InternalAutoSaveContext : IAutoSaveContext, IAutoSaveContentProvider<string>
    {
        public string WorkingPath { get; set; } = Path.Combine(Environment.CurrentDirectory, ".autosave");
        public string RawFilePath { get; set; } = string.Empty;
        public string Content => _contentProvider?.Content ?? string.Empty;

        IAutoSaveContentProvider<string>? _contentProvider;

        public InternalAutoSaveContext(IAutoSaveContentProvider<string>? contentProvider)
        {
            _contentProvider = contentProvider;
        }
        public InternalAutoSaveContext()
        {

        }
    }
    class InternalAutoSaveContentProvider: IAutoSaveContentProvider<string>
    {
        public string Content { get; set; } = string.Empty;
    }
}
