using System.Media;
using System.Windows;
using Dusk.Core;

namespace Dusk;

/// <summary>
/// The last-minute heads-up. Enter snoozes, Esc cancels: the safe choices are
/// the easy ones, and doing it right now is the quiet secondary option.
/// </summary>
public partial class WarningWindow : Window
{
    private readonly TimerController _timer;

    public WarningWindow(TimerController timer)
    {
        InitializeComponent();
        _timer = timer;
        var action = timer.Current?.Action ?? PowerAction.Shutdown;
        Headline.Text = $"Bilgisayar birazdan {PowerActionText.Future(action)}";
        Detail.Text = action is PowerAction.Sleep
            ? "Açık işleriniz olduğu gibi kalır."
            : "Açık işlerinizi kaydetmek için iyi bir an.";
        NowButton.Content = PowerActionText.Now(action);

        _timer.Changed += OnChanged;
        _timer.Ended += OnEnded;
        Closed += (_, _) => { _timer.Changed -= OnChanged; _timer.Ended -= OnEnded; };
        Loaded += (_, _) => { SystemSounds.Exclamation.Play(); SnoozeButton.Focus(); };
        OnChanged(this, EventArgs.Empty);
    }

    private void OnChanged(object? sender, EventArgs e)
    {
        var c = _timer.Current;
        if (c is null) return;
        var now = DateTime.UtcNow;
        var left = c.Remaining(now);
        if (left > Countdown.WarningLead) { Close(); return; } // snoozed from elsewhere
        Seconds.Text = ((int)Math.Ceiling(left.TotalSeconds)).ToString();
        Ring.FractionLeft = left.TotalSeconds / Countdown.WarningLead.TotalSeconds;
    }

    private void OnEnded(object? sender, string? reason) => Close();

    private void Snooze_Click(object sender, RoutedEventArgs e) => _timer.Extend(10);
    private void Cancel_Click(object sender, RoutedEventArgs e) => _timer.Cancel();
    private void Now_Click(object sender, RoutedEventArgs e) => _timer.RunNow();
}
