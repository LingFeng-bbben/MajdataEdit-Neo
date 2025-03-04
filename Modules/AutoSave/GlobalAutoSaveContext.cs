/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;

namespace MajdataEdit_Neo.Modules.AutoSave;
/// <summary>
///     全局自动保存上下文
/// </summary>
public class GlobalAutoSaveContext : IAutoSaveContext
{
    public string GetSavePath()
    {
        return Environment.CurrentDirectory + "/.autosave";
    }
}