/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using MajdataEdit_Neo.Modules.AutoSave.Contexts;

namespace MajdataEdit_Neo.Modules.AutoSave.Saver;
/// <summary>
///     自动保存行为接口
///     职责仅为进行自动保存行为
/// </summary>
public interface IAutoSaver
{
    IAutoSaveContext Context { get; }
    /// <summary>
    ///     执行自动保存行为
    /// </summary>
    /// <returns>是否成功保存</returns>
    bool DoAutoSave();
}