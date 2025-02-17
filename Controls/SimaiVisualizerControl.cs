using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using MajdataEdit_Neo.Models;
using MajSimai;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace MajdataEdit_Neo.Controls;

class SimaiVisualizerControl : Control
{
    //Set the properties
    //The naming of this should be strictly followed "Xxx" and "XxxProperty"
    public static readonly DirectProperty<SimaiVisualizerControl, double> TimeProperty =
    AvaloniaProperty.RegisterDirect<SimaiVisualizerControl, double>(
        nameof(Time),
        o => o.Time,
        (o,v)=>o.Time = v,
        defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    private double _time;
    public double Time
    {
        get { return _time; }
        set { SetAndRaise(TimeProperty, ref _time, value); }
    }

    public static readonly DirectProperty<SimaiVisualizerControl, TrackInfo> TrackIfProperty =
    AvaloniaProperty.RegisterDirect<SimaiVisualizerControl, TrackInfo>(
        nameof(TrackIf),
        o => o.TrackIf,
        (o, v) => o.TrackIf = v,
        defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    private TrackInfo _track;
    public TrackInfo TrackIf
    {
        get { return _track; }
        set { SetAndRaise(TrackIfProperty, ref _track, value); }
    }

    public static readonly DirectProperty<SimaiVisualizerControl, float> ZoomLevelProperty =
    AvaloniaProperty.RegisterDirect<SimaiVisualizerControl, float>(
        nameof(ZoomLevel),
        o => o.ZoomLevel,
        (o, v) => o.ZoomLevel = v,
        defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    private float _zoomLevel;
    public float ZoomLevel
    {
        get { return _zoomLevel; }
        set { SetAndRaise(ZoomLevelProperty, ref _zoomLevel, value); }
    }

    public static readonly DirectProperty<SimaiVisualizerControl, SimaiChart> SimaiChartProperty =
    AvaloniaProperty.RegisterDirect<SimaiVisualizerControl, SimaiChart>(
        nameof(SimaiChart),
        o => o.SimaiChart,
        (o, v) => o.SimaiChart = v,
        defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    private SimaiChart _simaiChart;
    public SimaiChart SimaiChart
    {
        get { return _simaiChart; }
        set { SetAndRaise(SimaiChartProperty, ref _simaiChart, value); }
    }

    public static readonly DirectProperty<SimaiVisualizerControl, float> OffsetProperty =
   AvaloniaProperty.RegisterDirect<SimaiVisualizerControl, float>(
       nameof(Offset),
       o => o.Offset,
       (o, v) => o.Offset = v,
       defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    private float _offset;
    public float Offset
    {
        get { return _offset; }
        set { SetAndRaise(OffsetProperty, ref _offset, value); }
    }

    public static readonly DirectProperty<SimaiVisualizerControl, double> CaretTimeProperty =
    AvaloniaProperty.RegisterDirect<SimaiVisualizerControl, double>(
        nameof(CaretTime),
        o => o.CaretTime,
        (o, v) => o.CaretTime = v,
        defaultBindingMode: Avalonia.Data.BindingMode.OneWay);
    private double _caretTime;
    public double CaretTime
    {
        get { return _caretTime; }
        set { SetAndRaise(CaretTimeProperty, ref _caretTime, value); }
    }

    //Override Render
    private readonly GlyphRun _noSkia;
    public SimaiVisualizerControl()
    {
        ClipToBounds = true;
        var text = "Current rendering API is not Skia";
        var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
        _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);

        AffectsRender<SimaiVisualizerControl>(TimeProperty, TrackIfProperty, ZoomLevelProperty, SimaiChartProperty, OffsetProperty, CaretTimeProperty);
    }
    class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference _noSkia;
        private readonly TrackInfo _trackInfo;
        private readonly SimaiChart _simaiChart;
        private readonly double _time;
        private readonly double _caretTime;
        private readonly float _zoomLevel;
        private readonly float _offset;
        private static double _lastTime;
        private static double _lastZoom;
        public CustomDrawOp(Rect bounds, GlyphRun noSkia, 
            TrackInfo trackInfo, double time, float zoomLevel,SimaiChart simaiChart,float offset, double caretTime)
        {
            _noSkia = noSkia.TryCreateImmutableGlyphRunReference();
            _trackInfo = trackInfo;
            _time = time;
            _zoomLevel = zoomLevel;
            _simaiChart = simaiChart;
            _offset = offset;
            _caretTime = caretTime;
            Bounds = bounds;
        }
        public void Dispose(){}
        public Rect Bounds { get; }
        public bool HitTest(Point p) => true;
        public bool Equals(ICustomDrawOperation other) => false;
        public void Render(ImmediateDrawingContext context)
        {
            if (_trackInfo is null) return;
            if (_simaiChart is null) return;
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null)
                context.DrawGlyphRun(Brushes.Red, _noSkia); //Some platform may not support it
            else
            {
                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;
                var paint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    Color = new SKColor(0, 100, 0, 150),
                    //StrokeCap = SKStrokeCap.Round
                };
                canvas.Save();
                var width = Bounds.Width;
                var height = Bounds.Height;
                //Actuall Drawing here
                //make it smooth
                _lastTime += 0.2 * (_time - _lastTime);
                _lastZoom += 0.2 * (_zoomLevel - _lastZoom);

                var waveLevels = _trackInfo.RawWave; 
                if (_lastZoom > 3) waveLevels = _trackInfo.GetWaveThumbnails(2);
                if (_lastZoom > 2) waveLevels = _trackInfo.GetWaveThumbnails(1);
                if (_lastZoom > 1) waveLevels = _trackInfo.GetWaveThumbnails(0);
                var songLength = _trackInfo.Length;
                
                var currentTime = _lastTime;
                var step = songLength / waveLevels.Length;
                var deltatime = _lastZoom;

                var startindex = (int)((currentTime - deltatime) / step);
                var stopindex = (int)((currentTime + deltatime) / step);
                var linewidth = (float)(width / (stopindex - startindex));
                var points = new List<SKPoint>();

                for (var i = startindex; i < stopindex; i++)
                {
                    if (i < 0) i = 0;
                    if (i >= waveLevels.Length - 1) break;

                    var x = (i - startindex) * linewidth;
                    var y = waveLevels[i] / 65535f * height + height / 2;

                    points.Add(new SKPoint((float)x, (float)y));
                }
                canvas.DrawPoints(SKPointMode.Polygon, points.ToArray(), paint);

                paint.IsAntialias = true;

                //Draw Bpm Lines
                var lastbpm = -1f;
                var bpmChangeTimes = new List<double>();
                var bpmChangeValues = new List<float>();

                //scan to get bpm change time and value
                foreach (var timing in _simaiChart.CommaTimings)
                {
                    if (timing.Bpm != lastbpm)
                    {
                        bpmChangeTimes.Add(timing.Timing+_offset);
                        bpmChangeValues.Add(timing.Bpm);
                        lastbpm = timing.Bpm;
                    }
                }
                bpmChangeTimes.Add(_trackInfo.Length);

                double time = bpmChangeTimes.FirstOrDefault(); //initial offset
                var signature = 4; // Time signature
                var currentBeat = 1;
                double timePerBeat;
                paint.Color = SKColors.Yellow;
                paint.StrokeWidth = 1;
                var strongBeat = new List<double>();
                var weakBeat = new List<double>();

                for (var i = 1; i < bpmChangeTimes.Count; i++)
                {
                    if (time - currentTime > deltatime) continue;
                    var x = ((float)(time / step) - startindex) * linewidth;
                    canvas.DrawText(bpmChangeValues[i - 1].ToString(),(float)x+3f,10,paint);


                    while (time < bpmChangeTimes[i] - 0.05)
                    {
                        if (currentBeat > signature) currentBeat = 1;
                        timePerBeat = 60.0 / bpmChangeValues[i - 1];

                        if (currentBeat == 1)
                            strongBeat.Add(time);
                        else
                            weakBeat.Add(time);

                        currentBeat++;
                        time += timePerBeat;
                    }

                    time = bpmChangeTimes[i];
                    currentBeat = 1;
                }

                foreach (var btime in strongBeat)
                {
                    if (btime - currentTime > deltatime) continue;
                    var x = ((float)(btime / step) - startindex) * linewidth;
                    canvas.DrawLine((float)x, 0, (float)x, (float)height, paint);
                }

                foreach (var btime in weakBeat)
                {
                    if (btime - currentTime > deltatime) continue;
                    var x = ((float)(btime / step) - startindex) * linewidth;
                    canvas.DrawLine((float)x, 0, (float)x, 10, paint);
                }

                //timing white line
                paint.Color = SKColors.White;
                foreach (var note in _simaiChart.CommaTimings)
                {
                    time = note.Timing + _offset;
                    if (time - currentTime > deltatime) continue;
                    var x = ((float)(time / step) - startindex) * linewidth;
                    canvas.DrawLine((float)x, (float)height -10, (float)x, (float)height, paint);
                }

                paint.Color = new SKColor(200, 0, 0, 200);
                paint.StrokeWidth = 2;
                canvas.DrawLine((float)width / 2, 15, (float)width / 2, (float)height-15, paint);

                paint.Style = SKPaintStyle.Stroke;
                // Draw notes
                foreach (var note in _simaiChart.NoteTimings)
                {
                    time = note.Timing + _offset;
                    if (time - currentTime > deltatime) continue;
                    var notes = note.Notes;
                    var isEach = notes.Count(o => !o.IsSlideNoHead) > 1;
                    var x = (float)(((float)(time / step) - startindex) * linewidth);

                    foreach (var noteD in notes)
                    {
                        var seprate = (height - 30f) / 8f;
                        var y = (float)(noteD.StartPosition * seprate + 10f);

                        switch (noteD.Type)
                        {
                            case SimaiNoteType.Tap:
                                paint.StrokeWidth = noteD.IsForceStar ? 3 : 2;
                                paint.Color = noteD.IsBreak ? SKColors.OrangeRed :
                                              isEach ? SKColors.Gold :
                                              SKColors.LightPink;

                                if (noteD.IsForceStar)
                                {
                                    canvas.DrawText("*", x - 7f, y - 7f, new SKPaint
                                    {
                                        Color = paint.Color,
                                        TextSize = 12,
                                        Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
                                    });
                                }
                                else
                                {
                                    canvas.DrawOval(x, y, 3.5f, 3.5f, paint);
                                }
                                break;

                            case SimaiNoteType.Touch:
                                paint.StrokeWidth = 2;
                                paint.Color = isEach ? SKColors.Gold : SKColors.DeepSkyBlue;
                                canvas.DrawRect(x - 2.5f, y - 2.5f, 7, 7, paint);
                                break;

                            case SimaiNoteType.Hold:
                                paint.StrokeWidth = 3.5f;
                                paint.Color = noteD.IsBreak ? SKColors.OrangeRed :
                                              isEach ? SKColors.Gold :
                                              SKColors.LightPink;

                                var xRight = (float)(x + (noteD.HoldTime / step) * linewidth);
                                if (!float.IsNormal(xRight)) xRight = ushort.MaxValue;
                                if (xRight - x < 1f) xRight = x + 5;
                                canvas.DrawLine(x, y, xRight, y, paint);
                                break;

                            case SimaiNoteType.TouchHold:
                                paint.StrokeWidth = 3.5f;
                                var xDelta = (float)(noteD.HoldTime / step) * linewidth / 4f;
                                if (!float.IsNormal(xDelta)) xDelta = ushort.MaxValue;
                                if (xDelta < 1f) xDelta = 1;

                                var colors = new[] { SKColors.Orange, SKColors.Yellow, SKColors.Green, SKColors.Blue };
                                for (var j = 0; j < 4; j++)
                                {
                                    paint.Color = colors[j];
                                    canvas.DrawLine(x, y, x + xDelta * (4 - j), y, paint);
                                }
                                break;

                            case SimaiNoteType.Slide:
                                paint.StrokeWidth = 1.5f;

                                if (!noteD.IsSlideNoHead)
                                {
                                    paint.Color = noteD.IsSlideBreak ? SKColors.OrangeRed :
                                                  isEach ? SKColors.Gold :
                                                  SKColors.DeepSkyBlue;
                                    var rad = 5f;
                                    var rad2 = rad * 1.414f / 2f;
                                    canvas.DrawLine(x - rad2, y - rad2, x + rad2, y + rad2, paint);
                                    canvas.DrawLine(x + rad2, y - rad2, x - rad2, y + rad2, paint);
                                    canvas.DrawLine(x, y - rad, x, y + rad, paint);
                                    canvas.DrawLine(x - rad, y, x + rad, y, paint);
                                }

                                paint.StrokeWidth = 3.5f;
                                paint.Color = noteD.IsSlideBreak ? SKColors.OrangeRed :
                                              notes.Count(o => o.Type == SimaiNoteType.Slide) >= 2 ? SKColors.Gold :
                                              SKColors.SkyBlue;
                                paint.PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0);
                                var xSlide = (float)((noteD.SlideStartTime+_offset) / step - startindex) * linewidth;
                                var xSlideRight = (float)(noteD.SlideTime / step) * linewidth + xSlide;

                                if (!float.IsNormal(xSlideRight)) xSlideRight = ushort.MaxValue;
                                if (!float.IsNormal(xSlide)) xSlide = ushort.MaxValue;

                                canvas.DrawLine(xSlide, y, xSlideRight, y, paint);
                                paint.PathEffect = null;
                                break;
                        }
                    }
                }

                time = _caretTime + _offset;
                if (time - currentTime <= deltatime)
                {
                    //Draw ghost cusor
                    paint.Color = SKColors.Orange;
                    paint.Style = SKPaintStyle.Fill;
                    var x2 = (float)(time / step - startindex) * linewidth;
                    SKPoint[] tranglePoints2 = { new(x2 - 5, 0), new(x2 + 5, 0), new(x2, 8f)};
                    var path = new SKPath();
                    path.MoveTo(tranglePoints2[0]);
                    foreach (var point in tranglePoints2) path.LineTo(point);
                    path.Close();
                    canvas.DrawPath(path, paint);
                }
                
                canvas.Restore();
            }
        }
    }
    public override void Render(DrawingContext context)
    {
        context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _noSkia,
            TrackIf, Time, ZoomLevel, SimaiChart, Offset, CaretTime));
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }
}
