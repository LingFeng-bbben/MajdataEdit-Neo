using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

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
    public static string PlayerAddress 
    {
        get => field;
        set
        {
            if (IsConnected)
                throw new InvalidOperationException();
            field = value;
            _client = new(value);
            _client.OnClose += OnClose;
            _client.OnOpen += OnOpen;
            _client.OnMessage += OnMessage;
            _client.OnError += OnError;
        } 
    } = "ws://localhost:8083/";


    static ViewSummary _playerSummary;
    static WebSocket _client = new("ws://127.0.0.1:8083");
    readonly static JsonSerializerOptions JSON_READER_OPTIONS = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
    };

    static PlayerConnection()
    {
        _client.OnClose += OnClose;
        _client.OnOpen += OnOpen;
        _client.OnMessage += OnMessage;
        _client.OnError += OnError;
    }
    static void OnOpen(object? sender, EventArgs args)
    {
        IsConnected = true;
    }
    static void OnClose(object? sender, CloseEventArgs args)
    {
        IsConnected = false;
    }
    static void OnMessage(object? sender, MessageEventArgs args)
    {

    }
    static void OnError(object? sender, ErrorEventArgs args)
    {

    }
    public static async Task<bool> ConnectAsync()
    {
        if (IsConnected)
            return true;
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(2000);

        return await ConnectToPlayer(cts.Token);
    }
    public static void Disconnect()
    {
        _client.Close();
    }
    public static async Task LoadAsync(byte[] track,
                                       byte[] cover,
                                       byte[] mv)
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Load,
            requestData = new MajWsRequestLoadBinary()
            {
                Image = cover,
                Track = track,
                Video = mv
            }
        };
        await SendAsync(req);
    }
    public static async Task LoadAsync(string trackPath,
                                       string coverPath,
                                       string mvPath)
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Load,
            requestData = new MajWsRequestLoad()
            {
                ImagePath = coverPath,
                TrackPath = trackPath,
                VideoPath = mvPath
            }
        };
        await SendAsync(req);
    }
    public static async Task ResetAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Reset,
            requestData = null
        };
        await SendAsync(req);
    }
    //public static async Task ParseMaidataAsync(string maidataContent,double startAt)
    //{
    //    EnsureConnectedToPlayer();
    //    var rsp = await _client.PostAsync(_maidataApiUri, new StringContent(maidataContent));
    //    rsp.EnsureSuccessStatusCode();
    //}
    public static async Task PauseAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Pause,
            requestData = null
        };
        await SendAsync(req);
    }
    public static async Task StopAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Stop,
            requestData = null
        };
        await SendAsync(req);
    }
    public static async Task PlayAsync(PlayRequest req)
    {
        //using var memoryStream = new MemoryStream();
        //await JsonSerializer.SerializeAsync(memoryStream,req);
        //var rsp = await _client.PostAsync(_playApiUri, new StreamContent(memoryStream));
        //rsp.EnsureSuccessStatusCode();
    }
    public static async Task ResumeAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Resume,
            requestData = null
        };
        await SendAsync(req);
    }
    static async Task<bool> ConnectToPlayer(CancellationToken token = default)
    {
        try
        {
            _client.ConnectAsync();
            while(!_client.IsAlive)
            {
                await Task.Yield();
                token.ThrowIfCancellationRequested();
            }
            return true;
        }
        catch
        {
            _client.Close();
            return false;
        }
    }
    static void EnsureConnectedToPlayer()
    {
        if (IsConnected)
            return;
        throw new PlayerNotConnectedException();
    }
    static async Task SendAsync(MajWsRequestBase req)
    {
        EnsureConnectedToPlayer();
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, req);
        _client.SendAsync(stream, (int)stream.Length, null);
    }
    readonly struct ViewSummary
    {
        public PlayerStatus State { get; init; }
        public string ErrMsg { get; init; }
        public float Timeline { get; init; }
    }
    readonly struct MajWsRequestBase
    {
        public MajWsRequestType requestType { get; init; }
        public object? requestData { get; init; }
    }
    readonly struct MajWsRequestLoad
    {
        /*public string TrackB64 { get; set; }
        public string ImageB64 { get; set; }
        public string VideoB64 { get; set; }*/
        public string TrackPath { get; init; }
        public string ImagePath { get; init; }
        public string VideoPath { get; init; }
    }
    readonly struct MajWsRequestLoadBinary
    {
        public byte[] Track { get; init; }
        public byte[] Image { get; init; }
        public byte[] Video { get; init; }
    }
    readonly struct MajWsRequestPlay
    {
        public double StartAt { get; init; }
        public string SimaiFumen { get; init; }
        public double Offset { get; init; }
    }
    enum MajWsRequestType
    {
        Reset,
        Load,
        Play,
        Pause,
        Resume,
        Stop,
        State
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

