/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit_Neo.Modules.AutoSave;
/// <summary>
///     本地自动保存上下文
/// </summary>
public class LocalAutoSaveContext : IAutoSaveContext
{
    public string GetSavePath()
    {
        // TODO: Maidata folder path

        //var maidataDir = MainWindow.maidataDir;
        //if (maidataDir.Length == 0) throw new LocalDirNotOpenYetException();

        //return MainWindow.maidataDir + "/.autosave";
        return string.Empty;
    }
}