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
    public IAutoSaveContext Context => _saveContext;

    readonly IAutoSaveIndexManager _indexManager;
    readonly IAutoSaveContext _saveContext;

    public LocalAutoSaver(IAutoSaveContext saveContext)
    {
        _saveContext = saveContext;
        _indexManager = new AutoSaveIndexManager(saveContext);
        _indexManager.SetMaxAutoSaveCount(AutoSaveManager.LOCAL_AUTOSAVE_MAX_COUNT);
    }


    public bool DoAutoSave()
    {
        // 本地自动保存前 总是尝试将当前目录更新到目前打开的文件夹上
        _indexManager.ChangePath(_saveContext.WorkingPath);

        var newSaveFilePath = _indexManager.GetNewAutoSaveFileName();

        File.WriteAllText(newSaveFilePath, _saveContext.Content);

        _indexManager.RefreshIndex();

        return true;
    }
}