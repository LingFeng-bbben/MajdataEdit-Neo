using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;

namespace MajdataEdit_Neo.Models;

class TrackReader : IDisposable
{
    public TrackReader()
    {
        Bass.Init();
    }

    public void Dispose()
    {
        Bass.Free();
    }

    public TrackInfo ReadTrack (string dirpath)
    {
        var useOgg = File.Exists(dirpath + "/track.ogg");
        var filePath = dirpath + "/track" + (useOgg ? ".ogg" : ".mp3");
        var bgmDecode = Bass.CreateStream(filePath, 0L, 0L, BassFlags.Decode);
        var bgmSample = Bass.SampleLoad(filePath, 0, 0, 1, BassFlags.Default);
        try
        {
            var songLength = Bass.ChannelBytes2Seconds(bgmDecode, Bass.ChannelGetLength(bgmDecode));
            var bgmInfo = Bass.SampleGetInfo(bgmSample);
            var freq = bgmInfo.Frequency;
            var sampleCount = (long)(songLength * freq * 2);
            var bgmRAW = new short[sampleCount];
            Bass.SampleGetData(bgmSample, bgmRAW);
            return new TrackInfo(songLength, bgmRAW);

        }
        catch (Exception e)
        {
            throw new Exception("mp3/ogg解码失败。\nMP3/OGG Decode fail.\n" + e.Message + Bass.LastError);
        }
        finally
        {
            Bass.StreamFree(bgmDecode);
            Bass.SampleFree(bgmSample);
        }
    }
}

public class TrackInfo
{
    public double Length { get; }
    public short[] RawWave { get; } = new short[0];
    private short[][] waveThumbnails = new short[3][];
    public short[] GetWaveThumbnails(int thumbLevel = 0)
    {
        if (thumbLevel < 0) return waveThumbnails[0];
        if (thumbLevel > 2) return waveThumbnails[2];
        return waveThumbnails[thumbLevel];
    }
    public TrackInfo(double length, short[] rawWave)
    {
        if (length == 0 || rawWave.Length == 0) throw new Exception("Music Wave Load Error");
        Length = length;
        RawWave = rawWave;

        var sampleCount = rawWave.Length;

        waveThumbnails[0] = new short[sampleCount / 20 + 1];
        for (var i = 0; i < sampleCount; i = i + 20) waveThumbnails[0][i / 20] = RawWave[i];
        waveThumbnails[1] = new short[sampleCount / 50 + 1];
        for (var i = 0; i < sampleCount; i = i + 50) waveThumbnails[1][i / 50] = RawWave[i];
        waveThumbnails[2] = new short[sampleCount / 100 + 1];
        for (var i = 0; i < sampleCount; i = i + 100) waveThumbnails[2][i / 100] = RawWave[i];

    }
}
