/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;

namespace MajdataEdit_Neo.Modules.AutoSave.Contexts;
/// <summary>
///     全局自动保存上下文
/// </summary>
internal class GlobalAutoSaveContext : IAutoSaveContext
{

    public string WorkingPath { get; } = Path.Combine(Environment.CurrentDirectory, ".autosave");
    public string Content => _contentProvider.Content;

    IAutoSaveContentProvider<string> _contentProvider;
    internal GlobalAutoSaveContext(IAutoSaveContentProvider<string> contentProvider)
    {
        _contentProvider = contentProvider;
    }
}