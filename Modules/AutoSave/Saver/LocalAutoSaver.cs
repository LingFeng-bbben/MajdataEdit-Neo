/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using MajdataEdit_Neo.Modules.AutoSave;
using MajdataEdit_Neo.Modules.AutoSave.Contexts;

namespace MajdataEdit_Neo.Modules.AutoSave.Saver;
/// <summary>
///     本地自动保存
///     它将自动保存的文件存储在当前谱面的目录中
/// </summary>
internal class LocalAutoSaver : IAutoSaver
{
    readonly IAutoSaveIndexManager _indexManager = new AutoSaveIndexManager();
    readonly IAutoSaveContext _saveContext;
    readonly AutoSaveManager _manager;

    public LocalAutoSaver()
    {
        _indexManager.SetMaxAutoSaveCount(AutoSaveManager.LOCAL_AUTOSAVE_MAX_COUNT);
        _manager = AutoSaveManager.Instance;
        _saveContext = _manager.LocalContext;
    }


    public bool DoAutoSave()
    {
        // 本地自动保存前 总是尝试将当前目录更新到目前打开的文件夹上
        _indexManager.ChangePath(_saveContext.WorkingPath);

        var newSaveFilePath = _indexManager.GetNewAutoSaveFileName();

        // TODO: FumenContentProvider

        //SimaiProcess.SaveData(newSaveFilePath);

        _indexManager.RefreshIndex();

        return true;
    }
}