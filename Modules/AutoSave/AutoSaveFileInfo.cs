/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit_Neo.Modules.AutoSave;

public class AutoSaveFileInfo
{
    /// <summary>
    ///     自动保存文件名
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    ///     原先的文件路径
    /// </summary>
    public string? RawPath { get; init; }

    /// <summary>
    ///     自动保存时间
    /// </summary>
    public long SavedTime { get; set; }
}