using System.Globalization;

namespace Dusk.Core;

public enum Lang { Tr, En }

/// <summary>
/// Every user-facing string, in Turkish and English. Turkish when Windows'
/// display language is Turkish, English otherwise. Sentences are whole
/// templates (not glued fragments) because word order differs between the two.
/// </summary>
public static class Loc
{
    public static Lang Current { get; set; } =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr" ? Lang.Tr : Lang.En;

    private static bool Tr => Current == Lang.Tr;

    // ---- Actions -------------------------------------------------------

    public static string Label(PowerAction a) => (a, Tr) switch
    {
        (PowerAction.Shutdown, true) => "Kapat",
        (PowerAction.Restart, true) => "Yeniden başlat",
        (PowerAction.Sleep, true) => "Uyku",
        (PowerAction.SignOut, true) => "Oturumu kapat",
        (PowerAction.Shutdown, false) => "Shut down",
        (PowerAction.Restart, false) => "Restart",
        (PowerAction.Sleep, false) => "Sleep",
        (PowerAction.SignOut, false) => "Sign out",
        _ => a.ToString(),
    };

    public static string Now(PowerAction a) => (a, Tr) switch
    {
        (PowerAction.Shutdown, true) => "Şimdi kapat",
        (PowerAction.Restart, true) => "Şimdi başlat",
        (PowerAction.Sleep, true) => "Şimdi uyut",
        (PowerAction.SignOut, true) => "Şimdi çık",
        (PowerAction.Shutdown, false) => "Shut down now",
        (PowerAction.Restart, false) => "Restart now",
        (PowerAction.Sleep, false) => "Sleep now",
        (PowerAction.SignOut, false) => "Sign out now",
        _ => a.ToString(),
    };

    private static string TrFuture(PowerAction a) => a switch
    {
        PowerAction.Restart => "yeniden başlayacak",
        PowerAction.Sleep => "uyku moduna geçecek",
        PowerAction.SignOut => "oturumu kapatacak",
        _ => "kapanacak",
    };

    private static string EnVerb3(PowerAction a) => a switch
    {
        PowerAction.Restart => "Restarts",
        PowerAction.Sleep => "Sleeps",
        PowerAction.SignOut => "Signs out",
        _ => "Shuts down",
    };

    /// <summary>"23:45'te kapanacak" / "Shuts down at 11:45 PM". <paramref name="at"/> comes from <see cref="TimeText.At"/>.</summary>
    public static string HappensAt(PowerAction a, string at) => Tr ? $"{at} {TrFuture(a)}" : $"{EnVerb3(a)} {at}";

    /// <summary>"30 dk sonra kapanacak" / "Shuts down in 30 min".</summary>
    public static string HappensIn(PowerAction a, string after) => Tr ? $"{after} sonra {TrFuture(a)}" : $"{EnVerb3(a)} in {after}";

    /// <summary>Start button: "Kapat · 30 dk sonra" / "Shut down in 30 min".</summary>
    public static string StartButton(PowerAction a, string after) => Tr ? $"{Label(a)} · {after} sonra" : $"{Label(a)} in {after}";

    public static string WarningHeadline(PowerAction a) => Tr
        ? $"Bilgisayar birazdan {TrFuture(a)}"
        : a switch
        {
            PowerAction.Restart => "Your PC will restart soon",
            PowerAction.Sleep => "Your PC will go to sleep soon",
            PowerAction.SignOut => "You'll be signed out soon",
            _ => "Your PC will shut down soon",
        };

    public static string WarningDetail(PowerAction a) => a == PowerAction.Sleep
        ? (Tr ? "Açık işleriniz olduğu gibi kalır." : "Everything stays open as it is.")
        : (Tr ? "Açık işlerinizi kaydetmek için iyi bir an." : "A good moment to save your work.");

    // ---- Fixed texts ---------------------------------------------------

    public static string HintToClock => Tr ? "saat için dokunun" : "tap for time of day";
    public static string HintToDuration => Tr ? "süre için dokunun" : "tap for duration";
    public static string Extend10 => Tr ? "+10 dk" : "+10 min";
    public static string Extend10Tip => Tr ? "Süreyi 10 dakika uzat" : "Add 10 minutes";
    public static string Cancel => Tr ? "İptal" : "Cancel";
    public static string CancelTimer => Tr ? "İptal et" : "Cancel";
    public static string Snooze10 => Tr ? "10 dk ertele" : "Snooze 10 min";
    public static string Force => Tr ? "Kaydedilmemiş işleri bekleme" : "Don't wait for unsaved work";
    public static string ForceTip => Tr
        ? "Açık uygulamalar kaydetmeyi sormadan kapatılır. Kapalıyken Windows, kaydedilmemiş iş varsa sizi bekler."
        : "Apps are closed without asking to save. When off, Windows waits for you if anything is unsaved.";
    public static string WhatToDo => Tr ? "Ne yapılsın" : "What to do";
    public static string DialName => Tr ? "Süre" : "Duration";
    public static string DialHelp => Tr
        ? "Halkayı sürükleyin ya da ok tuşları ve fare tekerleğiyle ayarlayın"
        : "Drag the ring, or use the arrow keys and mouse wheel";
    public static string Left(string clock) => Tr ? $"{clock} kaldı" : $"{clock} left";

    public static string DryRunBanner => Tr
        ? "Deneme kipi: hiçbir şey kapanmayacak, yalnız günlüğe yazılacak."
        : "Trial mode: nothing will shut down; it is only logged.";
    public static string DryRunDone(PowerAction a) => Tr
        ? $"Deneme kipi: \"{Label(a)}\" yapılmış sayıldı."
        : $"Trial mode: \"{Label(a)}\" counted as done.";
    public static string Missed(string at) => Tr
        ? $"Bilgisayar {at} sırasında uykudaydı. Sürpriz olmasın diye zamanlayıcı iptal edildi."
        : $"Your PC was asleep at {at}. The timer was cancelled so it won't surprise you.";
    public static string Failed(string why) => (Tr ? "Yapılamadı: " : "Couldn't do it: ") + why;
    public static string FailedTitle => Tr ? "Dusk yapamadı" : "Dusk couldn't do it";
    public static string Refused(int code) => Tr ? $"Windows isteği reddetti (kod {code})." : $"Windows refused (code {code}).";
    public static string SleepRefused => Tr ? "Windows uyku isteğini reddetti." : "Windows refused to sleep.";
    public static string ShutdownExeMissing => Tr ? "shutdown.exe başlatılamadı." : "Couldn't start shutdown.exe.";

    public static string TrayIdle => Tr ? "Dusk · zamanlayıcı kapalı" : "Dusk · no timer";
    public static string TrayShow => Tr ? "Dusk'ı göster" : "Show Dusk";
    public static string TrayExtend => Tr ? "10 dakika uzat" : "Add 10 minutes";
    public static string TrayCancel => Tr ? "Zamanlayıcıyı iptal et" : "Cancel timer";
    public static string TrayQuit => Tr ? "Çık" : "Quit";
    public static string BackgroundTitle => Tr ? "Dusk arka planda" : "Dusk is still running";
    public static string BackgroundText => Tr
        ? "Zamanlayıcı çalışmaya devam ediyor. Simgeye tıklayarak geri açabilirsiniz."
        : "The timer keeps going. Click the icon to bring Dusk back.";
}
