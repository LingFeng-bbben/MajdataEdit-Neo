﻿using System;
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
using MajdataEdit_Neo.Utils;
using Avalonia.Threading;
using System.Collections.Concurrent;
using MsBox.Avalonia.Enums;

namespace MajdataEdit_Neo.Models;
internal class PlayerConnection : IDisposable
{
    public bool IsConnected => _client.IsAlive;
    public ViewSummary ViewSummary => _viewSummary;
    private ViewSummary _viewSummary;

    public delegate void NotifyPlayStartedEventHandler(object sender, MajWsResponseType e);
    public event NotifyPlayStartedEventHandler? OnPlayStarted;

    bool _lastState = false;
    Task _listenerTask = Task.CompletedTask;
    WebSocket _client = new("ws://127.0.0.1:8083/majdata");
    ConcurrentQueue<MessageEventArgs> _playerMessages = new();
    
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
            await Task.Run(async () =>
            {
                _client = new WebSocket(url);
                _client.OnClose += OnClose;
                _client.OnOpen += OnOpen;
                _client.OnMessage += OnMessage;
                _client.OnError += OnError;
                _client.Connect();
                while (!_client.IsAlive)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
                if(_listenerTask.IsCompleted)
                    _listenerTask = Task.Run(StartToListenWebSocket);
            });
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
        _lastState = true;
    }
    void OnClose(object? sender, CloseEventArgs args)
    {
        if (!_lastState)
            return;
        Dispatcher.UIThread.Invoke(async () =>
        {
            await MessageBox.ShowAsync("Player disconnected", "Warning", icon: Icon.Warning);
        });
        _lastState = false;
    }
    void OnMessage(object? sender, MessageEventArgs args)
    {
        _playerMessages.Enqueue(args);
    }
    void OnError(object? sender, ErrorEventArgs args)
    {
        Debug.WriteLine(args);
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
        if (ViewSummary.State != ViewStatus.Idle && ViewSummary.State != ViewStatus.Loaded) throw new InvalidOperationException();
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
        _viewSummary = new ViewSummary()
        {
            State = ViewStatus.Loaded,//hack
        };
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Stop,
            requestData = null
        };
        await SendAsync(req);
    }
    public async Task ParseAndPlayAsync(double startAt, double offset, string fumen,float speed=1)
    {
        if (ViewSummary.State != ViewStatus.Loaded && ViewSummary.State != ViewStatus.Error) throw new InvalidOperationException();
        var req = new MajWsRequestBase()
        {
            requestType = MajWsRequestType.Play,
            requestData = new MajWsRequestPlay()
            {
                Offset = offset,
                SimaiFumen = fumen,
                StartAt = startAt,
                Speed = speed
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
        var json = JsonSerializer.Serialize(req, JSON_READER_OPTIONS);
        await Task.Run(() => _client.Send(json));
    }
    void EnsureConnectedToPlayer()
    {
        if (IsConnected)
            return;
        throw new PlayerNotConnectedException();
    }
    async Task StartToListenWebSocket()
    {
        while(IsConnected)
        {
            try
            {
                while(_playerMessages.TryDequeue(out var args))
                {
                    var resp = JsonSerializer.Deserialize<MajWsResponseBase>(args.Data, JSON_READER_OPTIONS);
                    switch (resp.responseType)
                    {
                        case MajWsResponseType.Heartbeat:
                            var status = JsonSerializer.Deserialize<ViewSummary>(resp.responseData?.ToString() ?? string.Empty, JSON_READER_OPTIONS);
                            _viewSummary = status;
                            break;
                        case MajWsResponseType.PlayResumed:
                        case MajWsResponseType.PlayStarted:
                            _viewSummary = new ViewSummary()
                            {
                                State = ViewStatus.Playing,//hack
                            };
                            OnPlayStarted?.Invoke(this, resp.responseType);
                            break;
                        case MajWsResponseType.Error:
                            await Dispatcher.UIThread.Invoke(async () => {
                                await MessageBox.ShowAsync(resp.responseData.ToString() ?? "Unknown Error", "Error", icon: Icon.Error);
                            });
                            break;
                        default:
                            Debug.WriteLine(args.Data);
                            break;
                    }
                }
            }
            finally
            {
                await Task.Delay(100);
            }
        }
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

