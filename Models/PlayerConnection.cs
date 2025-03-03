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
using MajdataPlay.View.Types;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
using System.Diagnostics;

namespace MajdataEdit_Neo.Models;
internal class PlayerConnection : IDisposable
{
    public bool IsConnected => _client.IsAlive;

    WebSocket _client = new("ws://127.0.0.1:8083/majdata");
    readonly static JsonSerializerOptions JSON_READER_OPTIONS = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        },
    };
    public async Task<bool> ConnectAsync(string url = "ws://127.0.0.1:8083/majdata")
    {
        if (IsConnected)
            return true;
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(2000);

        return await ConnectToPlayer(url, cts.Token);
    }
    private async Task<bool> ConnectToPlayer(string url, CancellationToken token = default)
    {
        try
        {
            _client = new WebSocket(url);
            _client.OnClose += OnClose;
            _client.OnOpen += OnOpen;
            _client.OnMessage += OnMessage;
            _client.OnError += OnError;
            _client.ConnectAsync();
            while (!_client.IsAlive)
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
    void OnOpen(object? sender, EventArgs args)
    {

    }
    void OnClose(object? sender, CloseEventArgs args)
    {

    }
    void OnMessage(object? sender, MessageEventArgs args)
    {
        Debug.WriteLine(args.Data);
    }
    void OnError(object? sender, ErrorEventArgs args)
    {

    }
/*    public async Task LoadAsync(byte[] track,
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
    }*/
    public async Task LoadAsync(string trackPath,
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
    public async Task ResetAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Reset,
            requestData = null
        };
        await SendAsync(req);
    }
    public async Task PauseAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Pause,
            requestData = null
        };
        await SendAsync(req);
    }
    public async Task StopAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Stop,
            requestData = null
        };
        await SendAsync(req);
    }
    public async Task PlayAsync(double startAt, double offset, string fumen)
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Stop,
            requestData = new MajWsRequestPlay()
            {
                Offset = offset,
                SimaiFumen = fumen,
                StartAt = startAt,
            }
        };
        await SendAsync(req);
    }
    public async Task ResumeAsync()
    {
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Resume,
            requestData = null
        };
        await SendAsync(req);
    }

    async Task SendAsync(MajWsRequestBase req)
    {
        EnsureConnectedToPlayer();
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, req);
        _client.SendAsync(stream, (int)stream.Length, null);
    }
    void EnsureConnectedToPlayer()
    {
        if (IsConnected)
            return;
        throw new PlayerNotConnectedException();
    }

    public void Dispose()
    {
        _client.Close();
    }
}
internal class PlayerNotConnectedException : Exception
{
    public PlayerNotConnectedException() : base() { }
}

