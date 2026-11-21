using Sukun.Core;

namespace Sukun.Tests;

public class CountdownTests
{
    private static readonly DateTime T0 = new(2026, 11, 21, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Ticks_then_warns_once_then_fires()
    {
        var c = new Countdown(PowerAction.Shutdown, TimeSpan.FromMinutes(5), T0);
        Assert.Equal(CountdownStep.Tick, c.Evaluate(T0.AddMinutes(1)));
        Assert.Equal(CountdownStep.Warn, c.Evaluate(T0.AddMinutes(4)));
        Assert.Equal(CountdownStep.Tick, c.Evaluate(T0.AddMinutes(4).AddSeconds(30)));
        Assert.Equal(CountdownStep.Fire, c.Evaluate(T0.AddMinutes(5)));
    }

    [Fact]
    public void Short_countdown_warns_immediately()
    {
        var c = new Countdown(PowerAction.Sleep, TimeSpan.FromSeconds(30), T0);
        Assert.Equal(CountdownStep.Warn, c.Evaluate(T0));
    }

    [Fact]
    public void Target_passed_while_asleep_is_missed_not_fired()
    {
        var c = new Countdown(PowerAction.Shutdown, TimeSpan.FromMinutes(30), T0);
        Assert.Equal(CountdownStep.Missed, c.Evaluate(T0.AddHours(3)));
    }

    [Fact]
    public void Small_lateness_still_fires()
    {
        var c = new Countdown(PowerAction.Shutdown, TimeSpan.FromMinutes(30), T0);
        Assert.Equal(CountdownStep.Fire, c.Evaluate(T0.AddMinutes(31)));
    }

    [Fact]
    public void Extend_during_warning_allows_a_new_warning_later()
    {
        var c = new Countdown(PowerAction.Shutdown, TimeSpan.FromMinutes(5), T0);
        var t = T0.AddMinutes(4).AddSeconds(30);
        Assert.Equal(CountdownStep.Warn, c.Evaluate(t));
        c.Extend(TimeSpan.FromMinutes(10), t);
        Assert.Equal(TimeSpan.FromSeconds(630), c.Remaining(t));
        Assert.Equal(1.0, c.FractionLeft(t));
        Assert.Equal(CountdownStep.Tick, c.Evaluate(t.AddMinutes(1)));
        Assert.Equal(CountdownStep.Warn, c.Evaluate(t.AddSeconds(580)));
    }

    [Fact]
    public void Fraction_and_remaining_are_clamped()
    {
        var c = new Countdown(PowerAction.Shutdown, TimeSpan.FromMinutes(10), T0);
        Assert.Equal(0.5, c.FractionLeft(T0.AddMinutes(5)), 6);
        Assert.Equal(TimeSpan.Zero, c.Remaining(T0.AddHours(1)));
        Assert.Equal(0, c.FractionLeft(T0.AddHours(1)));
    }

    [Fact]
    public void Rejects_non_positive_duration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Countdown(PowerAction.Shutdown, TimeSpan.Zero, T0));
    }
}
