/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using MajdataEdit_Neo.Modules.AutoSave.Contexts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MajdataEdit_Neo.Modules.AutoSave;
internal class AutoSaveIndexManager : IAutoSaveIndexManager
{
    string? _curPath;
    AutoSaveIndex? _index;
    bool _isReady;
    int _maxAutoSaveCount;

    readonly IAutoSaveContext _context;

    public AutoSaveIndexManager(IAutoSaveContext context)
    {
        _maxAutoSaveCount = 5;
        _context = context;
    }

    public AutoSaveIndexManager(IAutoSaveContext context, int maxAutoSaveCount) : this(context)
    {
        this._maxAutoSaveCount = maxAutoSaveCount;
    }

    public void ChangePath(string path)
    {
        if (path != _curPath)
        {
            // 只有当新目录和之前设置的目录不同时，才会触发index文件读写
            _curPath = path;
            LoadOrCreateIndexFile();
        }

        _isReady = true;
    }

    public int GetFileCount()
    {
        if (!IsReady()) throw new AutoSaveIndexNotReadyException("AutoSaveIndexManager is not ready yet.");

        return _index!.Count;
    }

    public List<AutoSaveFileInfo> GetFileInfos()
    {
        if (!IsReady()) throw new AutoSaveIndexNotReadyException("AutoSaveIndexManager is not ready yet.");

        return _index!.FilesInfo;
    }

    public int GetMaxAutoSaveCount()
    {
        return _maxAutoSaveCount;
    }

    public string GetNewAutoSaveFileName()
    {
        var path = _curPath + "/autosave." + GetCurrentTimeString() + ".txt";

        var fileInfo = new AutoSaveFileInfo
        {
            FileName = path,
            SavedTime = DateTimeOffset.Now.AddHours(8).ToUnixTimeSeconds(),
            RawPath = _context.RawFilePath
        };
        _index!.FilesInfo.Add(fileInfo);

        _index.Count++;

        // 将变更存储到index文件中
        UpdateIndexFile();

        return path;
    }

    public bool IsReady()
    {
        return _isReady;
    }

    public void RefreshIndex()
    {
        // 先扫描一遍，如果有文件已经被删了就先移除掉
        for (var i = _index!.Count - 1; i >= 0; i--)
        {
            var fileInfo = _index.FilesInfo[i];
            if (!File.Exists(fileInfo.FileName))
            {
                _index.FilesInfo.RemoveAt(i);
                _index.Count--;
            }
        }

        // 然后从this.index.FileInfo的表头开始删除 直到保证自动保存文件的数量符合maxAutoSaveCount的要求
        while (_index.Count > _maxAutoSaveCount)
        {
            var fileInfo = _index.FilesInfo[0];
            File.Delete(fileInfo.FileName!);
            _index.FilesInfo.RemoveAt(0);
            _index.Count--;
        }

        // 将变更存储到index文件中
        UpdateIndexFile();
    }

    public void SetMaxAutoSaveCount(int maxAutoSaveCount)
    {
        this._maxAutoSaveCount = maxAutoSaveCount;
        Console.WriteLine("maxAutoSaveCount:" + maxAutoSaveCount);
    }


    private void LoadOrCreateIndexFile()
    {
        CreateDirectoryIfNotExists(_curPath!);
        KeepDirectoryHidden(_curPath!);

        var indexFilePath = _curPath + "/.index.json";
        if (!File.Exists(indexFilePath))
        {
            _index = new AutoSaveIndex();
            UpdateIndexFile();
        }
        else
        {
            LoadIndexFromFile();
        }
    }


    /// <summary>
    ///     若文件夹不存在则创建
    /// </summary>
    /// <param name="dirPath"></param>
    private void CreateDirectoryIfNotExists(string dirPath)
    {
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
    }

    /// <summary>
    ///     保证文件夹处于隐藏状态
    /// </summary>
    /// <param name="dirPath"></param>
    private void KeepDirectoryHidden(string dirPath)
    {
        var dirInfo = new DirectoryInfo(dirPath);

        if ((dirInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
            dirInfo.Attributes = FileAttributes.Hidden;
    }

    /// <summary>
    ///     将saveIndex存储到index文件中
    /// </summary>
    private void UpdateIndexFile()
    {
        var indexPath = _curPath + "/.index.json";

        //var jsonText = JsonConvert.SerializeObject(index);
        var jsonText = JsonSerializer.Serialize(_index);
        File.WriteAllText(indexPath, jsonText);
    }

    /// <summary>
    ///     从index文件读取saveIndex
    /// </summary>
    private void LoadIndexFromFile()
    {
        var indexPath = _curPath + "/.index.json";

        var jsonText = File.ReadAllText(indexPath);
        //index = JsonConvert.DeserializeObject<AutoSaveIndex>(jsonText);
        _index = JsonSerializer.Deserialize<AutoSaveIndex>(jsonText);
    }

    /// <summary>
    ///     获取当前时间字符串
    /// </summary>
    /// <returns></returns>
    private string GetCurrentTimeString()
    {
        var now = DateTime.Now;

        return now.Year + "-" +
               now.Month + "-" +
               now.Day + "_" +
               now.Hour + "-" +
               now.Minute + "-" +
               now.Second;
    }
}