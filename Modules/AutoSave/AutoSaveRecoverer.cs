/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using MajdataEdit_Neo.Modules.AutoSave.Contexts;
using System.Collections.Generic;
using System.IO;

namespace MajdataEdit_Neo.Modules.AutoSave;
internal class AutoSaveRecoverer : IAutoSaveRecoverer
{
    readonly IAutoSaveContext _globalContext;
    readonly IAutoSaveIndexManager _globalIndex;
    readonly IAutoSaveContext _localContext;
    readonly IAutoSaveIndexManager _localIndex;

    readonly AutoSaveManager _manager;

    readonly static IReadOnlyCollection<AutoSaveFileInfo> EMPTY_COLLECTION = new List<AutoSaveFileInfo>(0);
    public AutoSaveRecoverer()
    {
        _manager = AutoSaveManager.Instance;
        _localContext = _manager.LocalContext;
        _globalContext = _manager.GlobalContext;
        _localIndex = new AutoSaveIndexManager(AutoSaveManager.LOCAL_AUTOSAVE_MAX_COUNT);
        try
        {
            _localIndex.ChangePath(_localContext.WorkingPath);
        }
        catch (LocalDirNotOpenYetException)
        {
        }

        _globalIndex = new AutoSaveIndexManager(AutoSaveManager.GLOBAL_AUTOSAVE_MAX_COUNT);
        _globalIndex.ChangePath(_globalContext.WorkingPath);
    }

    public IReadOnlyCollection<AutoSaveFileInfo> GetLocalAutoSaves()
    {
        try
        {
            _localIndex.ChangePath(_localContext.WorkingPath);
        }
        catch (LocalDirNotOpenYetException)
        {
            return EMPTY_COLLECTION;
        }
        var result = new List<AutoSaveFileInfo>();

        result.AddRange(_localIndex.GetFileInfos());
        result.Sort(delegate(AutoSaveFileInfo f1, AutoSaveFileInfo f2)
        {
            return f2.SavedTime.CompareTo(f1.SavedTime);
        });

        return result;
    }

    public IReadOnlyCollection<AutoSaveFileInfo> GetGlobalAutoSaves()
    {
        var result = new List<AutoSaveFileInfo>();
        result.AddRange(_globalIndex.GetFileInfos());
        result.Sort(delegate(AutoSaveFileInfo f1, AutoSaveFileInfo f2)
        {
            return f2.SavedTime.CompareTo(f1.SavedTime);
        });

        return result;
    }

    public IReadOnlyCollection<AutoSaveFileInfo> GetAllAutoSaves()
    {
        var result = new List<AutoSaveFileInfo>();

        result.AddRange(GetLocalAutoSaves());
        result.AddRange(GetGlobalAutoSaves());

        return result;
    }
    public bool RecoverFile(AutoSaveFileInfo recoveredFileInfo)
    {
        // 原始的maidata路径
        var rawMaidataPath = recoveredFileInfo.RawPath + "/maidata.txt";
        // 原始maidata恢复前备份路径
        var backupMaidataPath = recoveredFileInfo.RawPath + "/maidata.before_recovery.txt";
        // 自动保存maidata路径
        var autosaveMaidataPath = recoveredFileInfo.FileName;

        try
        {
            // 删除之前的备份（若有）
            if (File.Exists(backupMaidataPath)) File.Delete(backupMaidataPath);
            // 备份恢复前的maidata
            File.Move(rawMaidataPath, backupMaidataPath);
            // 将自动保存maidata恢复到原目录
            File.Copy(autosaveMaidataPath!, rawMaidataPath);
        }
        catch
        {
            return false;
        }

        return true;
    }
}