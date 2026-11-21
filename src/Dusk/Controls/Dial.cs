using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using Dusk.Core;

namespace Dusk.Controls;

/// <summary>
/// The round timer dial. Idle: drag around the ring to choose minutes
/// (one lap is an hour, laps stack up to 12 hours). Running: the ring drains.
/// </summary>
public sealed class Dial : FrameworkElement
{
    public static readonly DependencyProperty MinutesProperty = DependencyProperty.Register(
        nameof(Minutes), typeof(int), typeof(Dial),
        new FrameworkPropertyMetadata(30, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            null, (_, v) => Math.Clamp((int)v, Settings.MinMinutes, Settings.MaxMinutes)));

    public static readonly DependencyProperty FractionLeftProperty = DependencyProperty.Register(
        nameof(FractionLeft), typeof(double), typeof(Dial),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning), typeof(bool), typeof(Dial),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(Dial),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x4A, 0x54, 0xC4)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(
        nameof(Track), typeof(Brush), typeof(Dial),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x24, 0x80, 0x80, 0x80)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TickProperty = DependencyProperty.Register(
        nameof(Tick), typeof(Brush), typeof(Dial),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x60, 0x80, 0x80, 0x80)), FrameworkPropertyMetadataOptions.AffectsRender));

    public int Minutes { get => (int)GetValue(MinutesProperty); set => SetValue(MinutesProperty, value); }
    public double FractionLeft { get => (double)GetValue(FractionLeftProperty); set => SetValue(FractionLeftProperty, value); }
    public bool IsRunning { get => (bool)GetValue(IsRunningProperty); set => SetValue(IsRunningProperty, value); }
    public Brush Accent { get => (Brush)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }
    public Brush Track { get => (Brush)GetValue(TrackProperty); set => SetValue(TrackProperty, value); }
    public Brush Tick { get => (Brush)GetValue(TickProperty); set => SetValue(TickProperty, value); }

    /// <summary>The middle of the dial was clicked (not the ring).</summary>
    public event EventHandler? CenterClicked;

    private bool _dragging;
    private double _dragRaw;      // unsnapped minutes while dragging
    private double _lastAngleMin; // pointer angle, in minutes (0..60)
    private static readonly Brush KnobFill = Brushes.White;

    public Dial()
    {
        Focusable = true;
        FocusVisualStyle = null; // we draw our own focus ring
        Cursor = Cursors.Arrow;
    }

    private double Size => Math.Min(ActualWidth, ActualHeight);
    private Point Center => new(ActualWidth / 2, ActualHeight / 2);
    private double Thickness => Math.Max(10, Size * 0.055);
    private double Radius => Size / 2 - Thickness * 1.1;

    protected override void OnRender(DrawingContext dc)
    {
        var c = Center;
        var r = Radius;
        var t = Thickness;

        // Transparent disc so the whole face receives the mouse.
        dc.DrawEllipse(Brushes.Transparent, null, c, Size / 2, Size / 2);

        // Hour ticks just inside the ring.
        var tickPen = new Pen(Tick, Math.Max(1.5, t * 0.14)) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        for (int i = 0; i < 12; i++)
        {
            double a = i / 12.0 * 2 * Math.PI;
            double inner = r - t * (i % 3 == 0 ? 1.9 : 1.55), outer = r - t * 1.05;
            dc.DrawLine(tickPen, Polar(c, inner, a), Polar(c, outer, a));
        }

        dc.DrawEllipse(null, new Pen(Track, t), c, r, r);

        if (IsRunning)
        {
            DrawArc(dc, Accent, c, r, t, FractionLeft);
        }
        else
        {
            int laps = Minutes / 60, rest = Minutes % 60;
            if (laps > 0)
            {
                var faded = Accent.CloneCurrentValue();
                faded.Opacity = rest == 0 ? 1 : 0.38;
                dc.DrawEllipse(null, new Pen(faded, t), c, r, r);
            }
            if (rest > 0) DrawArc(dc, Accent, c, r, t, rest / 60.0);

            // Knob at the end of the arc.
            double frac = rest == 0 ? 1 : rest / 60.0;
            var k = Polar(c, r, frac * 2 * Math.PI);
            var shadow = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
            dc.DrawEllipse(shadow, null, new Point(k.X, k.Y + 1.5), t * 0.95, t * 0.95);
            dc.DrawEllipse(KnobFill, null, k, t * 0.85, t * 0.85);
        }

        if (_showFocusRing && IsKeyboardFocused && !IsRunning)
            dc.DrawEllipse(null, new Pen(Accent, 2) { DashStyle = DashStyles.Dot }, c, r + t, r + t);
    }

    private static void DrawArc(DrawingContext dc, Brush brush, Point c, double r, double t, double fraction)
    {
        fraction = Math.Clamp(fraction, 0, 1);
        var pen = new Pen(brush, t) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (fraction >= 0.9999) { dc.DrawEllipse(null, pen, c, r, r); return; }
        if (fraction <= 0.0001) return;

        var start = Polar(c, r, 0);
        var end = Polar(c, r, fraction * 2 * Math.PI);
        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.ArcTo(end, new Size(r, r), 0, fraction > 0.5, SweepDirection.Clockwise, true, true);
        }
        g.Freeze();
        dc.DrawGeometry(null, pen, g);
    }

    /// <summary>Angle 0 is 12 o'clock, growing clockwise.</summary>
    private static Point Polar(Point c, double r, double angle) =>
        new(c.X + r * Math.Sin(angle), c.Y - r * Math.Cos(angle));

    private double AngleInMinutes(Point p)
    {
        var c = Center;
        double a = Math.Atan2(p.X - c.X, -(p.Y - c.Y)); // -pi..pi, 0 at top
        if (a < 0) a += 2 * Math.PI;
        return a / (2 * Math.PI) * 60;
    }

    private bool IsOnRing(Point p)
    {
        var d = (p - Center).Length;
        return d >= Radius - Thickness * 2.6 && d <= Radius + Thickness * 2;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var p = e.GetPosition(this);
        if (!_dragging)
        {
            Cursor = IsRunning ? Cursors.Arrow : IsOnRing(p) ? Cursors.Hand : Cursors.Arrow;
            return;
        }

        double a = AngleInMinutes(p);
        double delta = a - _lastAngleMin;
        if (delta > 30) delta -= 60;      // crossed 12 o'clock backwards
        else if (delta < -30) delta += 60; // crossed 12 o'clock forwards
        _lastAngleMin = a;
        _dragRaw = Math.Clamp(_dragRaw + delta, Settings.MinMinutes, Settings.MaxMinutes);
        Minutes = (int)Math.Round(_dragRaw);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        if (IsRunning) return;
        var p = e.GetPosition(this);
        if (IsOnRing(p))
        {
            _dragging = true;
            _dragRaw = Minutes;
            _lastAngleMin = AngleInMinutes(p);
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragging)
        {
            _dragging = false;
            ReleaseMouseCapture();
        }
        else if (!IsOnRing(e.GetPosition(this)) && (e.GetPosition(this) - Center).Length < Radius)
        {
            CenterClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (IsRunning) return;
        Minutes = StepTo(Minutes, e.Delta > 0 ? 5 : -5);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsRunning) return;
        int step = e.Key switch
        {
            Key.Up or Key.Right => 1,
            Key.Down or Key.Left => -1,
            Key.PageUp => 15,
            Key.PageDown => -15,
            Key.Home => -Settings.MaxMinutes,
            Key.End => Settings.MaxMinutes,
            _ => 0,
        };
        if (step == 0) return;
        _showFocusRing = true;
        Minutes = Math.Clamp(Minutes + step, Settings.MinMinutes, Settings.MaxMinutes);
        e.Handled = true;
    }

    /// <summary>Wheel steps land on multiples of 5 so values stay round.</summary>
    private static int StepTo(int value, int step)
    {
        int next = step > 0 ? (value / 5 + 1) * 5 : (value - 1) / 5 * 5;
        return Math.Clamp(next, 5, Settings.MaxMinutes);
    }

    // The focus ring is for keyboard users only, like :focus-visible on the web.
    private bool _showFocusRing;

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        _showFocusRing = InputManager.Current.MostRecentInputDevice is KeyboardDevice;
        InvalidateVisual();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        _showFocusRing = false;
        InvalidateVisual();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new DialAutomationPeer(this);

    /// <summary>Lets screen readers (and UI tests) read and set the minutes.</summary>
    private sealed class DialAutomationPeer(Dial owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private Dial D => (Dial)Owner;
        protected override string GetClassNameCore() => nameof(Dial);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
        public override object GetPattern(PatternInterface p) =>
            p == PatternInterface.RangeValue ? this : base.GetPattern(p);

        public double Value => D.IsRunning ? Math.Round(D.FractionLeft * 100) : D.Minutes;
        public bool IsReadOnly => D.IsRunning;
        public double Maximum => D.IsRunning ? 100 : Settings.MaxMinutes;
        public double Minimum => D.IsRunning ? 0 : Settings.MinMinutes;
        public double LargeChange => 15;
        public double SmallChange => 1;
        public void SetValue(double value)
        {
            if (IsReadOnly) throw new ElementNotEnabledException();
            D.Minutes = (int)Math.Round(value);
        }
    }
}
