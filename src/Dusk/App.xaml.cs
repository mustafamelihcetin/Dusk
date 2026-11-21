using System.Threading;
using System.Windows;
using Dusk.Core;
using Dusk.Services;

namespace Dusk;

public partial class App : Application
{
    private const string MutexName = @"Local\Dusk.SingleInstance";
    private const string ActivateEventName = @"Local\Dusk.Activate";

    private Mutex? _mutex;
    private EventWaitHandle? _activate;
    private TimerController? _timer;
    private TrayIcon? _tray;
    private MainWindow? _main;
    private WarningWindow? _warning;
    private bool _toldAboutTray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // One Dusk at a time: a second launch just brings the first forward.
        _mutex = new Mutex(true, MutexName, out bool first);
        if (!first)
        {
            try { EventWaitHandle.OpenExisting(ActivateEventName).Set(); } catch (WaitHandleCannotBeOpenedException) { }
            Shutdown();
            return;
        }
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        new Thread(() =>
        {
            while (_activate.WaitOne()) Dispatcher.BeginInvoke(() => _main?.BringToFront());
        }) { IsBackground = true, Name = "Dusk.Activate" }.Start();

        PowerService.DryRun = e.Args.Contains("--deneme", StringComparer.OrdinalIgnoreCase)
            || Environment.GetEnvironmentVariable("DUSK_DENEME") == "1";

        var settings = Settings.Load(Settings.DefaultPath);
        _timer = new TimerController();
        _main = new MainWindow(_timer, settings);
        _tray = new TrayIcon(_timer);

        _tray.ShowRequested += (_, _) => _main.BringToFront();
        _tray.QuitRequested += (_, _) => Quit();
        _timer.Warning += (_, _) => ShowWarning();
        _timer.Ended += (_, reason) =>
        {
            if (reason is not null && !_main.IsVisible) _tray.Balloon("Dusk", reason);
        };
        _timer.Failed += (_, msg) =>
        {
            _main.BringToFront();
            _tray.Balloon("Dusk yapamadı", msg);
        };
        _main.HiddenToTray += (_, _) =>
        {
            if (_toldAboutTray) return;
            _toldAboutTray = true;
            _tray.Balloon("Dusk arka planda", "Zamanlayıcı çalışmaya devam ediyor. Simgeye tıklayarak geri açabilirsiniz.");
        };
        _main.Closed += (_, _) => Quit();

        if (PowerService.DryRun)
            _main.ShowNotice("Deneme kipi: hiçbir şey kapanmayacak, yalnız günlüğe yazılacak.");
        _main.Show();
    }

    private void ShowWarning()
    {
        if (_warning is { IsLoaded: true }) { _warning.Activate(); return; }
        _warning = new WarningWindow(_timer!);
        _warning.Closed += (_, _) => _warning = null;
        _warning.Show();
        _warning.Activate();
    }

    private void Quit()
    {
        if (_main is not null)
        {
            _main.CurrentSettings.Save(Settings.DefaultPath);
            _main.AllowClose = true;
        }
        _timer?.Cancel();
        _tray?.Dispose();
        _tray = null;
        _main?.Close();
        Shutdown();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        // Windows is shutting down on its own; leave quietly.
        _main?.CurrentSettings.Save(Settings.DefaultPath);
        _tray?.Dispose();
        base.OnSessionEnding(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
