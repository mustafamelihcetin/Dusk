using System.Windows.Threading;
using Dusk.Core;
using Dusk.Services;

namespace Dusk;

/// <summary>
/// Owns the single running countdown and turns its steps into events for the
/// windows and the tray. Lives on the UI thread.
/// </summary>
public sealed class TimerController
{
    private readonly DispatcherTimer _timer;
    private Countdown? _countdown;
    private bool _force;

    public TimerController()
    {
        // Fast enough for a smooth ring; all decisions use the wall clock anyway.
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => OnTick();
    }

    public Countdown? Current => _countdown;
    public bool IsRunning => _countdown is not null;

    /// <summary>Every tick and every state change.</summary>
    public event EventHandler? Changed;
    /// <summary>Warning period started.</summary>
    public event EventHandler? Warning;
    /// <summary>Countdown ended without running (cancelled, missed, or done).</summary>
    public event EventHandler<string?>? Ended;
    /// <summary>The action could not be performed.</summary>
    public event EventHandler<string>? Failed;

    public void Start(PowerAction action, int minutes, bool force)
    {
        _countdown = new Countdown(action, TimeSpan.FromMinutes(minutes), DateTime.UtcNow);
        _force = force;
        KeepAwake.On();
        _timer.Start();
        OnTick();
    }

    public void Extend(int minutes)
    {
        if (_countdown is null) return;
        _countdown.Extend(TimeSpan.FromMinutes(minutes), DateTime.UtcNow);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Cancel(string? reason = null) => Stop(reason);

    /// <summary>Skip the rest of the countdown.</summary>
    public void RunNow()
    {
        if (_countdown is null) return;
        Fire(_countdown.Action);
    }

    private void OnTick()
    {
        if (_countdown is null) return;
        switch (_countdown.Evaluate(DateTime.UtcNow))
        {
            case CountdownStep.Warn:
                Changed?.Invoke(this, EventArgs.Empty);
                Warning?.Invoke(this, EventArgs.Empty);
                break;
            case CountdownStep.Fire:
                Fire(_countdown.Action);
                break;
            case CountdownStep.Missed:
                var at = TimeText.TimeOfDay(_countdown.TargetUtc.ToLocalTime());
                Stop($"Bilgisayar {at} sırasında uykudaydı. Sürpriz olmasın diye zamanlayıcı iptal edildi.");
                break;
            default:
                Changed?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void Fire(PowerAction action)
    {
        var force = _force;
        Stop(null);
        try
        {
            PowerService.Execute(action, force);
            if (PowerService.DryRun)
                Ended?.Invoke(this, $"Deneme kipi: \"{PowerActionText.Label(action)}\" yapılmış sayıldı.");
        }
        catch (Exception e)
        {
            Failed?.Invoke(this, e.Message);
        }
    }

    private void Stop(string? reason)
    {
        _timer.Stop();
        _countdown = null;
        KeepAwake.Off();
        Changed?.Invoke(this, EventArgs.Empty);
        Ended?.Invoke(this, reason);
    }
}
