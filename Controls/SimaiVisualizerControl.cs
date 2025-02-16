using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using MajdataEdit_Neo.Models;
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

    //Override Render
    private readonly GlyphRun _noSkia;
    public SimaiVisualizerControl()
    {
        ClipToBounds = true;
        var text = "Current rendering API is not Skia";
        var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
        _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);

        AffectsRender<SimaiVisualizerControl>(TimeProperty, TrackIfProperty, ZoomLevelProperty);
    }
    class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference _noSkia;
        private readonly TrackInfo _trackInfo;
        private readonly double _time;
        private readonly float _zoomLevel;
        private static double _lastTime;
        private static double _lastZoom;
        public CustomDrawOp(Rect bounds, GlyphRun noSkia, TrackInfo trackInfo, double time, float zoomLevel)
        {
            _noSkia = noSkia.TryCreateImmutableGlyphRunReference();
            _trackInfo = trackInfo;
            _time = time;
            _zoomLevel = zoomLevel;
            Bounds = bounds;
        }
        public void Dispose(){}
        public Rect Bounds { get; }
        public bool HitTest(Point p) => true;
        public bool Equals(ICustomDrawOperation other) => false;
        public void Render(ImmediateDrawingContext context)
        {
            if (_trackInfo is null) return;
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
                    Color = new SKColor(0,100,0,150),
                };
                canvas.Save();
                var width = Bounds.Width;
                var height = Bounds.Height;
                //Actuall Drawing here
                //make it smooth
                _lastTime += 0.1 * (_time - _lastTime);
                _lastZoom += 0.1 * (_zoomLevel - _lastZoom);

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
                var linewidth = width / (float)(stopindex - startindex);
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

                //Todo: Draw Notes Here

                canvas.Restore();
            }
        }
    }
    public override void Render(DrawingContext context)
    {
        context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _noSkia,
            TrackIf, Time, ZoomLevel));
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }
}
