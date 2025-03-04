/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.Collections.Generic;
using System.IO;

namespace MajdataEdit_Neo.Modules.AutoSave;
internal class AutoSaveRecoverer : IAutoSaveRecoverer
{
    private readonly IAutoSaveContext globalContext = new GlobalAutoSaveContext();
    private readonly IAutoSaveIndexManager globalIndex;
    private readonly IAutoSaveContext localContext = new LocalAutoSaveContext();
    private readonly IAutoSaveIndexManager localIndex;

    public AutoSaveRecoverer()
    {
        localIndex = new AutoSaveIndexManager(AutoSaveManager.LOCAL_AUTOSAVE_MAX_COUNT);
        try
        {
            localIndex.ChangePath(localContext.GetSavePath());
        }
        catch (LocalDirNotOpenYetException)
        {
        }

        globalIndex = new AutoSaveIndexManager(AutoSaveManager.GLOBAL_AUTOSAVE_MAX_COUNT);
        globalIndex.ChangePath(globalContext.GetSavePath());
    }

    public List<AutoSaveFileInfo> GetLocalAutoSaves()
    {
        var result = new List<AutoSaveFileInfo>();

        try
        {
            localIndex.ChangePath(localContext.GetSavePath());
        }
        catch (LocalDirNotOpenYetException)
        {
            return result;
        }

        result.AddRange(localIndex.GetFileInfos());
        result.Sort(delegate(AutoSaveFileInfo f1, AutoSaveFileInfo f2)
        {
            return f2.SavedTime.CompareTo(f1.SavedTime);
        });

        return result;
    }

    public List<AutoSaveFileInfo> GetGlobalAutoSaves()
    {
        var result = new List<AutoSaveFileInfo>();
        result.AddRange(globalIndex.GetFileInfos());
        result.Sort(delegate(AutoSaveFileInfo f1, AutoSaveFileInfo f2)
        {
            return f2.SavedTime.CompareTo(f1.SavedTime);
        });

        return result;
    }

    public List<AutoSaveFileInfo> GetAllAutoSaves()
    {
        var result = new List<AutoSaveFileInfo>();

        result.AddRange(GetLocalAutoSaves());
        result.AddRange(GetGlobalAutoSaves());

        return result;
    }

    public FumenInfos GetFumenInfos(string path)
    {
        return FumenInfos.FromFile(path);
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