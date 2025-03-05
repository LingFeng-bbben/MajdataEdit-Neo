/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using MajdataEdit_Neo.Modules.AutoSave.Contexts;
using MajdataEdit_Neo.Modules.AutoSave.Saver;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MajdataEdit_Neo.Modules.AutoSave;
/// <summary>
///     自动保存管理类
///     **单例运行**
///     其提供自动保存行为的计时能力，同时管理IAutoSave实现类的对象
/// </summary>
public sealed class AutoSaveManager
{
    public delegate void AutoSaveExecutedEventHandler(object? sender);
    /// <summary>
    /// Called when autosaving, this event uses threads
    /// </summary>
    public event AutoSaveExecutedEventHandler? OnAutoSaveExecuted;
    /// <summary>
    /// 指示是否启用AutoSave功能
    /// </summary>
    public bool Enabled 
    { 
        get => _enabled; 
        set
        {
            EnsureManagerIsInitialized();
            if (value)
            {
                _autoSaveTimer.Start();
            }
            else
            {
                _autoSaveTimer.Stop();
            }
        }
    }
    /// <summary>
    /// 自上次保存后，是否产生了修改
    /// </summary>
    public bool IsFileChanged
    {
        get => _isFileChanged;
        set => _isFileChanged = value;
    }
    /// <summary>
    /// 获取或设置自动保存Timer间隔
    /// </summary>
    public double Interval
    {
        get => _autoSaveTimer.Interval;
        set => _autoSaveTimer.Interval = value;
    }
    public IAutoSaveRecoverer Recoverer
    {
        get
        {
            EnsureManagerIsInitialized();
            return _recoverer;
        }
    }

    public static AutoSaveManager Instance
    {
        get
        {
            EnsureManagerIsInitialized();

            return _instance;
        }
    }
    public static Lock SyncLock => _syncLock;

    bool _isFileChanged = false;
    bool _enabled = false;

    AutoSaveRecoverer _recoverer;
    /// <summary>
    ///     自动保存计时Timer 默认每60秒检查一次
    /// </summary>
    readonly Timer _autoSaveTimer = new(1000 * 10);
    readonly List<IAutoSaver> _autoSavers = new();

    static volatile AutoSaveManager? _instance;
    static readonly Lock _syncLock = new();
    static readonly Lock _autoSavesSyncLock = new();

    internal static readonly int LOCAL_AUTOSAVE_MAX_COUNT = 5;
    internal static readonly int GLOBAL_AUTOSAVE_MAX_COUNT = 30;

    /// <summary>
    ///     构造函数
    /// </summary>
    AutoSaveManager(params IList<IAutoSaver> autoSavers)
    {
        if (autoSavers.Count < 2)
            throw new ArgumentException("The number of savers must be greater than 2", nameof(autoSavers));
        _instance = this;
        _recoverer = new AutoSaveRecoverer(autoSavers[0].Context, autoSavers[1].Context);
        // 本地存储者和全局存储者
        if(autoSavers is not null)
        {
            _autoSavers.AddRange(autoSavers);
        }

        // 存储事件
        _autoSaveTimer.AutoReset = true;
        _autoSaveTimer.Elapsed += autoSaveTimer_Elapsed;
    }
    public static void Initialize(IAutoSaveContext localAutoSaveContext, IAutoSaveContext globalAutoSaveContext)
    {
        lock(_syncLock)
        {
            if (_instance is not null)
                return;
            Initialize(new LocalAutoSaver(localAutoSaveContext), new GlobalAutoSaver(globalAutoSaveContext));
        }
    }
    public static void Initialize(params IList<IAutoSaver> autoSavers)
    {
        lock (_syncLock)
        {
            if (_instance is not null)
                return;
            _instance = new AutoSaveManager(autoSavers);
        }
    }
    /// <summary>
    /// Timer触发事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void autoSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        // 若文件未改动，则跳过此次自动保存
        if (!IsFileChanged || _autoSavers.Count == 0) 
            return;

        // 执行保存行为
        lock(_autoSavesSyncLock)
        {
            foreach (var saver in _autoSavers)
                saver.DoAutoSave();
        }
        Debug.WriteLine("Module: AutoSave executed");
        Task.Run(() => OnAutoSaveExecuted?.Invoke(this));
        // 标记变更已被保存
        _isFileChanged = false;
    }
    public void AddSaver<T>(T autoSaver) where T : IAutoSaver
    {
        lock(_autoSavesSyncLock)
        {
            _autoSavers.Add(autoSaver);
        }
    }
    public void AddRange<T>(T autoSavers) where T : IEnumerable<IAutoSaver>
    {
        lock (_autoSavesSyncLock)
        {
            _autoSavers.AddRange(autoSavers);
        }
    }
    [MemberNotNull(nameof(_instance))]
    static void EnsureManagerIsInitialized()
    {
        if (_instance is not null)
        {
            return;
        }

        throw new NotInitializedException();
    }
}