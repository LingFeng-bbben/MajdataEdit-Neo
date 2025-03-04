/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit_Neo.Modules.AutoSave.Contexts;
/// <summary>
///     本地自动保存上下文
/// </summary>
internal class LocalAutoSaveContext : IAutoSaveContext
{
    public string WorkingPath { get; } = string.Empty;
    public string Content => _contentProvider.Content;

    IAutoSaveContentProvider<string> _contentProvider;
    internal LocalAutoSaveContext(IAutoSaveContentProvider<string> contentProvider)
    {
        _contentProvider = contentProvider;
    }
}