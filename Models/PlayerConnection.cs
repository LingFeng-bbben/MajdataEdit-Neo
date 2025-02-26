using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.Models;
internal static class PlayerConnection
{
    public static PlayerStatus State
    {
        get
        {
            EnsureConnectedToPlayer();
            return _playerSummary.State;
        }
    }
    public static float Timeline
    {
        get
        {
            EnsureConnectedToPlayer();
            return _playerSummary.Timeline;
        }
    }
    public static string ErrMsg
    {
        get
        {
            return _playerSummary.ErrMsg;
        }
    }
    public static bool IsConnected { get; private set; } = false;
    public static Uri PlayerAddress 
    {
        get => field;
        set
        {
            if (IsConnected)
                throw new InvalidOperationException();
            field = value;
            _stateApiUri = new Uri(value, "/api/state");
            _loadApiUri = new Uri(value, "/api/load");
            _maidataApiUri = new Uri(value, "/api/maidata");
            _timestampApiUri = new Uri(value, "/api/timestamp");
            _playApiUri = new Uri(value, "/api/play");
            _pauseApiUri = new Uri(value, "/api/pause");
            _stopApiUri = new Uri(value, "/api/stop");
            _resetApiUri = new Uri(value, "/api/reset");
        } 
    } = new Uri("http://localhost:8013/");

    static Uri _stateApiUri = new Uri(PlayerAddress, "/api/state");
    static Uri _loadApiUri = new Uri(PlayerAddress, "/api/load");
    static Uri _maidataApiUri = new Uri(PlayerAddress, "/api/maidata");
    static Uri _timestampApiUri = new Uri(PlayerAddress, "/api/timestamp");
    static Uri _playApiUri = new Uri(PlayerAddress, "/api/play");
    static Uri _resumeApiUri = new Uri(PlayerAddress, "/api/resume");
    static Uri _pauseApiUri = new Uri(PlayerAddress, "/api/pause");
    static Uri _stopApiUri = new Uri(PlayerAddress, "/api/stop");
    static Uri _resetApiUri = new Uri(PlayerAddress, "/api/reset");

    static ViewSummary _playerSummary;
    static Task _keepAliveTask = Task.CompletedTask;
    readonly static HttpClient _client = new HttpClient(new HttpClientHandler()
    {
        Proxy = null,
        UseProxy = false,
    });
    static CancellationTokenSource _cts = new();
    public static async Task<bool> ConnectAsync()
    {
        if (!_keepAliveTask.IsCompleted)
            return true;
        _cts = new();
        _keepAliveTask = ConnectToPlayer();
        return await Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            cts.CancelAfter(2000);
            try
            {
                while (!IsConnected)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
    public static void Disconnect()
    {
        _cts.Cancel();
    }
    public static async Task LoadAsync(byte[] track,
                                       byte[] cover,
                                       byte[] mv)
    {
        EnsureConnectedToPlayer();
        
        var req = new HttpRequestMessage(HttpMethod.Post, _loadApiUri)
        {
            Content = new MultipartFormDataContent()
            {
                { new ByteArrayContent(track),"track" },
                { new ByteArrayContent(cover),"bg" },
                { new ByteArrayContent(mv),"bga" },
            }
        };
        var rsp = await _client.SendAsync(req);
        rsp.EnsureSuccessStatusCode();
    }
    public static async Task ResetAsync()
    {
        EnsureConnectedToPlayer();
        var rsp = await _client.GetAsync(_resetApiUri);
        rsp.EnsureSuccessStatusCode();
    }
    public static async Task ParseMaidataAsync(string maidataContent)
    {
        EnsureConnectedToPlayer();
        var rsp = await _client.PostAsync(_maidataApiUri, new StringContent(maidataContent));
        rsp.EnsureSuccessStatusCode();
    }
    public static async Task PauseAsync()
    {
        EnsureConnectedToPlayer();
        var rsp = await _client.GetAsync(_pauseApiUri);
        rsp.EnsureSuccessStatusCode();
    }
    public static async Task StopAsync()
    {
        EnsureConnectedToPlayer();
        var rsp = await _client.GetAsync(_stopApiUri);
        rsp.EnsureSuccessStatusCode();
    }
    public static async Task PlayAsync(PlayRequest req)
    {
        using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream,req);
        var rsp = await _client.PostAsync(_playApiUri, new StreamContent(memoryStream));
        rsp.EnsureSuccessStatusCode();
    }
    public static async Task ResumeAsync()
    {
        var rsp = await _client.GetAsync(_resumeApiUri);
        rsp.EnsureSuccessStatusCode();
    }
    static async Task ConnectToPlayer()
    {
        var helloPacket = new HttpRequestMessage(HttpMethod.Get, _stateApiUri)
        {
            Content = new StringContent("Hello!!!")
        };
        var token = _cts.Token;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(2000);
                    var rsp = await _client.SendAsync(helloPacket, cts.Token);
                    rsp.EnsureSuccessStatusCode();
                    var contentStream = await rsp.Content.ReadAsStreamAsync();
                    _playerSummary = await JsonSerializer.DeserializeAsync<ViewSummary>(contentStream);
                    IsConnected = true;
                }
                catch
                {
                    IsConnected = false;
                }
                finally
                {
                    await Task.Delay(1000);
                }
            }
        }
        finally
        {
            IsConnected = false;
        }
    }
    static void EnsureConnectedToPlayer()
    {
        if (IsConnected)
            return;
        throw new PlayerNotConnectedException();
    }
    struct ViewSummary
    {
        public PlayerStatus State { get; init; }
        public string ErrMsg { get; init; }
        public float Timeline { get; init; }
    }
}
internal enum PlaybackMode
{
    Normal,
    IncludeOp,
    Record
}
internal readonly struct PlayRequest
{
    public PlaybackMode Mode { get; init; }
    public float PlaybackSpeed { get; init; }
    public float StartAt { get; init; }
}
enum PlayerStatus
{
    Idle,
    Loaded,
    Ready,
    Error,
    Playing,
    Paused,
    Busy
}
internal class PlayerNotConnectedException : Exception
{
    public PlayerNotConnectedException() : base() { }
}

