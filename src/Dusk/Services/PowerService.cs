using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Dusk.Core;

namespace Dusk.Services;

/// <summary>
/// Performs the power action. Nothing is ever pre-scheduled in Windows: the
/// action happens only when this method is called, so quitting or killing the
/// app always cancels it.
/// </summary>
public static class PowerService
{
    /// <summary>
    /// Trial mode (--deneme or DUSK_DENEME=1): logs what would happen instead of doing it.
    /// </summary>
    public static bool DryRun { get; set; }

    public static string DryRunLog => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dusk", "deneme.log");

    public static void Execute(PowerAction action, bool force)
    {
        if (DryRun)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DryRunLog)!);
            File.AppendAllText(DryRunLog, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {action}  force={force}{Environment.NewLine}");
            return;
        }

        switch (action)
        {
            case PowerAction.Shutdown:
                // /hybrid keeps Fast Startup working exactly as the Start menu does.
                RunShutdownExe(force ? "/s /hybrid /f /t 0" : "/s /hybrid /t 0");
                break;
            case PowerAction.Restart:
                RunShutdownExe(force ? "/r /f /t 0" : "/r /t 0");
                break;
            case PowerAction.SignOut:
                RunShutdownExe(force ? "/l /f" : "/l");
                break;
            case PowerAction.Sleep:
                // hibernate=false, forceCritical=false (ignored on modern Windows), disableWake=false
                if (!SetSuspendState(false, false, false))
                    throw new InvalidOperationException(Loc.SleepRefused);
                break;
        }
    }

    private static void RunShutdownExe(string args)
    {
        var exe = Path.Combine(Environment.SystemDirectory, "shutdown.exe");
        using var p = Process.Start(new ProcessStartInfo(exe, args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException(Loc.ShutdownExeMissing);
        p.WaitForExit(10_000);
        if (p.HasExited && p.ExitCode != 0)
            throw new InvalidOperationException(Loc.Refused(p.ExitCode));
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);
}
