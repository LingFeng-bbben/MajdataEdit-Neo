/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.Collections.Generic;

namespace MajdataEdit_Neo.Modules.AutoSave;

/// <summary>
///     自动保存索引 用于索引当前环境中自动保存的文件
/// </summary>
public class AutoSaveIndex
{
    /// <summary>
    ///     已存在的自动保存文件数量
    /// </summary>
    public int Count = 0;

    /// <summary>
    ///     自动保存文件列表
    /// </summary>
    public List<AutoSaveFileInfo> FilesInfo = new();
}