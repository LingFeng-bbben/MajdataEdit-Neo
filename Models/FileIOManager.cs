using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.Models;

class FileIOManager
{
    public static async Task<IStorageFile?> DoOpenFilePickerAsync(FileOpenerType type)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");
        FilePickerFileType fptype;
        switch (type)
        {
            case FileOpenerType.Maidata:
                fptype = new FilePickerFileType("Maidata") { Patterns = ["maidata.txt"], MimeTypes = ["text/plain"] };
                break;
            case FileOpenerType.Track:
                fptype = new FilePickerFileType("Track") { Patterns = ["maidata.txt"], MimeTypes = ["text/plain"] };
                break;
            default:
                fptype = new FilePickerFileType("Null");
                break;
        }

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = $"Open {Enum.GetName(typeof(FileOpenerType),type)}",
            FileTypeFilter = [fptype],
            AllowMultiple = false
        });

        if (files.Count == 0) Debug.WriteLine("FileSelection Canceled");

        return files?.Count >= 1 ? files[0] : null;
    }

    public enum FileOpenerType
    {
        Maidata, Track, Image
    }
}

