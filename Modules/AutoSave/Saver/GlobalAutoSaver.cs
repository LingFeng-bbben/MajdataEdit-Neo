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
    readonly IAutoSaveIndexManager _indexManager;
    readonly IAutoSaveContext _saveContext;
    public GlobalAutoSaver(IAutoSaveContext saveContext)
    {
        _saveContext = saveContext;
        _indexManager = new AutoSaveIndexManager(saveContext);
        _indexManager.ChangePath(_saveContext.WorkingPath);
        _indexManager.SetMaxAutoSaveCount(AutoSaveManager.GLOBAL_AUTOSAVE_MAX_COUNT);
    }


    public bool DoAutoSave()
    {
        var newSaveFilePath = _indexManager.GetNewAutoSaveFileName();

        File.WriteAllText(newSaveFilePath, _saveContext.Content);

        _indexManager.RefreshIndex();

        return true;
    }
}