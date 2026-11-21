namespace Dusk.Core;

/// <summary>What happens when the countdown reaches zero.</summary>
public enum PowerAction
{
    Shutdown,
    Restart,
    Sleep,
    SignOut,
}

public static class PowerActionText
{
    /// <summary>Short label for the segmented control.</summary>
    public static string Label(PowerAction a) => a switch
    {
        PowerAction.Shutdown => "Kapat",
        PowerAction.Restart => "Yeniden başlat",
        PowerAction.Sleep => "Uyku",
        PowerAction.SignOut => "Oturumu kapat",
        _ => a.ToString(),
    };

    /// <summary>Verb used in sentences: "Bilgisayar 23:45'te {kapanacak}."</summary>
    public static string Future(PowerAction a) => a switch
    {
        PowerAction.Shutdown => "kapanacak",
        PowerAction.Restart => "yeniden başlayacak",
        PowerAction.Sleep => "uyku moduna geçecek",
        PowerAction.SignOut => "oturumu kapatacak",
        _ => a.ToString(),
    };

    /// <summary>Button text for doing it right away.</summary>
    public static string Now(PowerAction a) => a switch
    {
        PowerAction.Shutdown => "Şimdi kapat",
        PowerAction.Restart => "Şimdi başlat",
        PowerAction.Sleep => "Şimdi uyut",
        PowerAction.SignOut => "Şimdi çık",
        _ => a.ToString(),
    };
}
