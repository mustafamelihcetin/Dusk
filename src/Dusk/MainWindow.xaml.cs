using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Dusk.Core;

namespace Dusk;

public partial class MainWindow : Window
{
    private readonly TimerController _timer;
    private Settings _settings;
    private PowerAction _action;
    private bool _showClock;

    /// <summary>Set by the app when it really wants to quit (tray "Çık").</summary>
    public bool AllowClose { get; set; }

    /// <summary>Raised when the window hides to the tray while a timer runs.</summary>
    public event EventHandler? HiddenToTray;

    public MainWindow(TimerController timer, Settings settings)
    {
        InitializeComponent();
        _timer = timer;
        _settings = settings;
        _action = settings.Action;
        _showClock = settings.ShowClockTime;
        ApplyTexts();

        foreach (var a in Enum.GetValues<PowerAction>())
        {
            // Columns share the width in proportion to their labels, so long
            // labels ("Yeniden başlat") get room without truncation.
            var label = Loc.Label(a);
            ActionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(label.Length + 4, GridUnitType.Star) });
            var rb = new RadioButton
            {
                Content = label,
                Tag = a,
                GroupName = "action",
                Style = (Style)FindResource("SegmentStyle"),
                IsChecked = a == _action,
            };
            rb.Checked += (_, _) => { _action = a; Refresh(); };
            Grid.SetColumn(rb, ActionBar.Children.Count);
            ActionBar.Children.Add(rb);
        }

        Dial.Minutes = settings.Minutes;
        ForceBox.IsChecked = settings.Force;
        Dial.CenterClicked += (_, _) => { _showClock = !_showClock; Refresh(); };
        DependencyPropertyDescriptor.FromProperty(Controls.Dial.MinutesProperty, typeof(Controls.Dial))
            .AddValueChanged(Dial, (_, _) => Refresh());

        _timer.Changed += (_, _) => Refresh();
        _timer.Ended += (_, reason) => ShowNotice(reason);
        _timer.Failed += (_, msg) => ShowNotice(Loc.Failed(msg));

        Loaded += (_, _) => Refresh();
    }

    private void ApplyTexts()
    {
        Preset15.Content = TimeText.Duration(15);
        Preset30.Content = TimeText.Duration(30);
        Preset60.Content = TimeText.Duration(60);
        Preset120.Content = TimeText.Duration(120);
        ExtendButton.Content = Loc.Extend10;
        ExtendButton.ToolTip = Loc.Extend10Tip;
        CancelButton.Content = Loc.Cancel;
        ForceBox.Content = Loc.Force;
        ForceBox.ToolTip = Loc.ForceTip;
        AutomationProperties.SetName(ActionBar, Loc.WhatToDo);
        AutomationProperties.SetName(Dial, Loc.DialName);
        AutomationProperties.SetHelpText(Dial, Loc.DialHelp);
    }

    public Settings CurrentSettings => _settings with
    {
        Action = _action,
        Minutes = Dial.Minutes,
        Force = ForceBox.IsChecked == true,
        ShowClockTime = _showClock,
    };

    private void Refresh()
    {
        var running = _timer.Current;
        bool isRunning = running is not null;
        var nowLocal = DateTime.Now;

        Dial.IsRunning = isRunning;
        ActionBar.IsEnabled = !isRunning;
        Presets.Visibility = isRunning ? Visibility.Hidden : Visibility.Visible;
        StartButton.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
        RunningButtons.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
        ForceBox.IsEnabled = !isRunning;

        if (running is not null)
        {
            var now = DateTime.UtcNow;
            Dial.FractionLeft = running.FractionLeft(now);
            var left = TimeText.Clock(running.Remaining(now));
            var at = TimeText.At(running.TargetUtc.ToLocalTime(), nowLocal);
            if (_showClock)
            {
                BigText.Text = TimeText.TimeOfDay(running.TargetUtc.ToLocalTime());
                SubText.Text = Loc.HappensIn(running.Action, left);
            }
            else
            {
                BigText.Text = left;
                SubText.Text = Loc.HappensAt(running.Action, at);
            }
            HintText.Text = "";
            Title = $"{left} · Dusk";
        }
        else
        {
            var target = nowLocal.AddMinutes(Dial.Minutes);
            var duration = TimeText.Duration(Dial.Minutes);
            if (_showClock)
            {
                BigText.Text = TimeText.TimeOfDay(target);
                SubText.Text = Loc.HappensIn(_action, duration);
            }
            else
            {
                BigText.Text = duration;
                SubText.Text = Loc.HappensAt(_action, TimeText.At(target, nowLocal));
            }
            HintText.Text = _showClock ? Loc.HintToDuration : Loc.HintToClock;
            StartButton.Content = Loc.StartButton(_action, duration);
            Title = "Dusk";
        }
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        Dial.Minutes = int.Parse((string)((Button)sender).Tag);
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        Notice.Visibility = Visibility.Collapsed;
        _settings = CurrentSettings;
        _timer.Start(_action, Dial.Minutes, ForceBox.IsChecked == true);
    }

    private void Extend_Click(object sender, RoutedEventArgs e) => _timer.Extend(10);

    private void Cancel_Click(object sender, RoutedEventArgs e) => _timer.Cancel("main window");

    public void ShowNotice(string? text)
    {
        if (string.IsNullOrEmpty(text)) { Notice.Visibility = Visibility.Collapsed; return; }
        Notice.Text = text;
        Notice.Visibility = Visibility.Visible;
    }

    public void BringToFront()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        Topmost = true;  // Windows only lets the foreground app steal focus;
        Topmost = false; // this nudge brings us forward reliably.
        Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (AllowClose) return;
        if (_timer.IsRunning)
        {
            // A running timer keeps going quietly in the tray.
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke(this, EventArgs.Empty);
        }
    }
}
