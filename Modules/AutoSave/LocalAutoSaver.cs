/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit_Neo.Modules.AutoSave;
/// <summary>
///     本地自动保存
///     它将自动保存的文件存储在当前谱面的目录中
/// </summary>
internal class LocalAutoSaver : IAutoSaver
{
    private readonly IAutoSaveIndexManager indexManager = new AutoSaveIndexManager();
    private readonly IAutoSaveContext saveContext = new LocalAutoSaveContext();

    public LocalAutoSaver()
    {
        indexManager.SetMaxAutoSaveCount(AutoSaveManager.LOCAL_AUTOSAVE_MAX_COUNT);
    }


    public bool DoAutoSave()
    {
        // 本地自动保存前 总是尝试将当前目录更新到目前打开的文件夹上
        indexManager.ChangePath(saveContext.GetSavePath());

        var newSaveFilePath = indexManager.GetNewAutoSaveFileName();

        // TODO: FumenContentProvider

        //SimaiProcess.SaveData(newSaveFilePath);

        indexManager.RefreshIndex();

        return true;
    }
}