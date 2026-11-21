namespace Sukun.Core;

/// <summary>What the countdown wants the UI to do on this tick.</summary>
public enum CountdownStep
{
    /// <summary>Keep counting.</summary>
    Tick,
    /// <summary>The warning period just began; show the warning once.</summary>
    Warn,
    /// <summary>Time is up: perform the action.</summary>
    Fire,
    /// <summary>
    /// The target passed long ago (the computer slept or the app was frozen).
    /// Acting now would surprise the user, so the countdown is dropped instead.
    /// </summary>
    Missed,
}

/// <summary>
/// A running countdown. Pure logic driven by wall-clock UTC instants, so it is
/// immune to timer drift and survives sleep: every decision compares "now"
/// with the fixed target.
/// </summary>
public sealed class Countdown
{
    public static readonly TimeSpan WarningLead = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan MissedGrace = TimeSpan.FromMinutes(2);

    private bool _warned;

    public Countdown(PowerAction action, TimeSpan duration, DateTime nowUtc)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        Action = action;
        StartUtc = nowUtc;
        TargetUtc = nowUtc + duration;
    }

    public PowerAction Action { get; }
    public DateTime StartUtc { get; private set; }
    public DateTime TargetUtc { get; private set; }
    public TimeSpan Total => TargetUtc - StartUtc;

    public TimeSpan Remaining(DateTime nowUtc)
    {
        var r = TargetUtc - nowUtc;
        return r < TimeSpan.Zero ? TimeSpan.Zero : r;
    }

    /// <summary>Fraction of time still left: 1 at the start, 0 at the end.</summary>
    public double FractionLeft(DateTime nowUtc)
    {
        var total = Total.TotalMilliseconds;
        return total <= 0 ? 0 : Math.Clamp(Remaining(nowUtc).TotalMilliseconds / total, 0, 1);
    }

    /// <summary>Adds time. The ring restarts from full so the extension is visible.</summary>
    public void Extend(TimeSpan by, DateTime nowUtc)
    {
        if (by <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(by));
        var remaining = Remaining(nowUtc) + by;
        StartUtc = nowUtc;
        TargetUtc = nowUtc + remaining;
        // Re-arm the warning unless we are still inside the warning window.
        _warned = _warned && remaining <= WarningLead;
    }

    public CountdownStep Evaluate(DateTime nowUtc)
    {
        if (nowUtc >= TargetUtc)
            return nowUtc - TargetUtc > MissedGrace ? CountdownStep.Missed : CountdownStep.Fire;

        if (!_warned && TargetUtc - nowUtc <= WarningLead)
        {
            _warned = true;
            return CountdownStep.Warn;
        }
        return CountdownStep.Tick;
    }
}
