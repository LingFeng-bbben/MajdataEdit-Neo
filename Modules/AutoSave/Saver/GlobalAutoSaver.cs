/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using MajdataEdit_Neo.Modules.AutoSave;
using MajdataEdit_Neo.Modules.AutoSave.Contexts;

namespace MajdataEdit_Neo.Modules.AutoSave.Saver;
/// <summary>
///     全局自动保存
///     它将自动保存的文件存储在majdata的根目录中
/// </summary>
internal class GlobalAutoSaver : IAutoSaver
{
    readonly IAutoSaveIndexManager _indexManager = new AutoSaveIndexManager();
    readonly IAutoSaveContext _saveContext;
    readonly AutoSaveManager _manager;
    public GlobalAutoSaver()
    {
        _manager = AutoSaveManager.Instance;
        _saveContext = _manager.GlobalContext;
        _indexManager.ChangePath(_saveContext.WorkingPath);
        _indexManager.SetMaxAutoSaveCount(AutoSaveManager.GLOBAL_AUTOSAVE_MAX_COUNT);
    }


    public bool DoAutoSave()
    {
        var newSaveFilePath = _indexManager.GetNewAutoSaveFileName();

        //SimaiProcess.SaveData(newSaveFilePath);

        _indexManager.RefreshIndex();

        return true;
    }
}