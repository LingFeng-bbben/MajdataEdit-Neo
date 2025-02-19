using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using MajdataEdit_Neo.Models;
using MajSimai;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit_Neo.ViewModels;

partial class ChartInfoViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? title;
    [ObservableProperty]
    private string? artist;

    [ObservableProperty]
    private ObservableCollection<SimaiCommand>? simaiCommands;

    public Bitmap Cover { 
        get
        {
            var coverPath = MaidataDir + "/bg.png";
            if(File.Exists(coverPath)) return new Bitmap(coverPath);
            coverPath = MaidataDir + "/bg.jpg";
            if (File.Exists(coverPath)) return new Bitmap(coverPath);
            return new Bitmap(AssetLoader.Open(new Uri("avares://MajdataEdit-Neo/Assets/dummy.png")));
        } 
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Cover))]
    private string maidataDir;
    public ChartInfoViewModel()
    {
        SimaiCommands = new();
        SimaiCommands.Add(new SimaiCommand("aaa1", "bbb5"));
        SimaiCommands.Add(new SimaiCommand("aaa2", "bbb6"));
        SimaiCommands.Add(new SimaiCommand("aaa3", "bbb7"));
        SimaiCommands.Add(new SimaiCommand("aaa4", "bbb8"));
    }

    public void AddNewCommand()
    {
        if (SimaiCommands is null) SimaiCommands = new();
        SimaiCommands.Add(new SimaiCommand("", ""));
    }
    public void DelCommand(SimaiCommand command)
    {
        if (SimaiCommands is null) throw new InvalidOperationException();
        SimaiCommands.Remove(command);
    }

    public async Task OpenBgCover()
    {
        try
        {
            var file = await FileIOManager.DoOpenFilePickerAsync(FileIOManager.FileOpenerType.Image);
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null || MaidataDir is null) return;
            File.Delete(MaidataDir + "/bg.jpg");
            File.Delete(MaidataDir + "/bg.png");
            if (path.EndsWith("jpg"))
                File.Copy(path, MaidataDir + "/bg.jpg", true);
            if (path.EndsWith("png"))
                File.Copy(path, MaidataDir + "/bg.png", true);
            OnPropertyChanged(nameof(Cover));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    public async Task OpenBgVideo()
    {
        try
        {
            var file = await FileIOManager.DoOpenFilePickerAsync(FileIOManager.FileOpenerType.Video);
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null || MaidataDir is null) return;
            File.Delete(MaidataDir + "/bg.mp4");
            File.Copy(path, MaidataDir + "/bg.mp4", true);
            //TODO:Make it MessageBox
            if (new FileInfo(path).Length > 20971520) Debug.WriteLine("File too large. Compress?");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }
}
