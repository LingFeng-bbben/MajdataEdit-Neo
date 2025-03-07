﻿/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.Collections.Generic;

namespace MajdataEdit_Neo.Modules.AutoSave;
public interface IAutoSaveRecoverer
{
    /// <summary>
    ///     获取本地的自动保存文件列表
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<AutoSaveFileInfo> GetLocalAutoSaves();

    /// <summary>
    ///     获取全局的自动保存文件列表
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<AutoSaveFileInfo> GetGlobalAutoSaves();

    /// <summary>
    ///     获取所有的自动保存文件列表 包括本地的和全局的
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<AutoSaveFileInfo> GetAllAutoSaves();

    /// <summary>
    ///     根据recoveredFileInfo恢复文件
    /// </summary>
    /// <param name="recoveredFileInfo"></param>
    /// <returns></returns>
    bool RecoverFile(AutoSaveFileInfo recoveredFileInfo);
}