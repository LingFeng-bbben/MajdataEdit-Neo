using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System;
using System.Linq;

namespace MajdataEdit_Neo.Views;

public partial class BpmTapWindow : Window
{
    private List<double> bpms = new();
    private DateTime lastTime = DateTime.MinValue;
    public BpmTapWindow()
    {
        InitializeComponent();
        Tap_Button.Focus();
    }

    private void Tap_Button_Click(object? sender, RoutedEventArgs e)
    {
        if (lastTime != DateTime.MinValue)
        {
            var delta = (DateTime.Now - lastTime).TotalSeconds;
            bpms.Add(60d / delta);
        }

        lastTime = DateTime.Now;
        double sum = 0;
        if (bpms.Count <= 0) return;
        if (bpms.Count > 20)
        {
            bpms.Remove(bpms.Min());
            bpms.Remove(bpms.Max());
        }
        foreach (var bpm in bpms) sum += bpm;
        var avg = sum / bpms.Count;

        Tap_Button_Text.Content = string.Format("{0:N1}", avg);
    }

    private void Reset_Button_Click(object? sender, RoutedEventArgs e)
    {
        bpms = new List<double>();
        lastTime = DateTime.MinValue;
        Tap_Button_Text.Content = "Tap";
    }
}