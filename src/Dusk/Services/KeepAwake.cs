using System.Runtime.InteropServices;

namespace Dusk.Services;

/// <summary>
/// While a countdown runs, keeps Windows from idling into sleep so the timer
/// can actually fire. The display may still turn off.
/// </summary>
public static class KeepAwake
{
    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;

    public static void On() => SetThreadExecutionState(EsContinuous | EsSystemRequired);
    public static void Off() => SetThreadExecutionState(EsContinuous);

    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
}
