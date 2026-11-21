using System.Globalization;

namespace Dusk.Core;

public static class TimeText
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>English times follow the user's regional format (11:45 PM or 23:45).</summary>
    public static CultureInfo EnCulture { get; set; } = CultureInfo.CurrentCulture;

    private static bool Tr => Loc.Current == Lang.Tr;

    /// <summary>A chosen duration in words: "45 dk" / "45 min", "2 sa 15 dk" / "2 h 15 min".</summary>
    public static string Duration(int minutes)
    {
        string h = Tr ? "sa" : "h", m = Tr ? "dk" : "min";
        if (minutes <= 0) return $"0 {m}";
        int hh = minutes / 60, mm = minutes % 60;
        if (hh == 0) return $"{mm} {m}";
        return mm == 0 ? $"{hh} {h}" : $"{hh} {h} {mm} {m}";
    }

    /// <summary>A live countdown: "4:59", "1:04:59".</summary>
    public static string Clock(TimeSpan t)
    {
        // Round up so the display reaches 0:00 exactly when the action fires.
        var s = (long)Math.Ceiling(Math.Max(0, t.TotalSeconds));
        long h = s / 3600, m = s % 3600 / 60, sec = s % 60;
        return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
    }

    /// <summary>Wall-clock time of day: "23:45" / "11:45 PM".</summary>
    public static string TimeOfDay(DateTime local) =>
        Tr ? local.ToString("HH:mm", TrCulture) : local.ToString("t", EnCulture);

    /// <summary>
    /// "23:45'te", "yarın 07:30'da" / "at 11:45 PM", "tomorrow at 7:30 AM".
    /// </summary>
    public static string At(DateTime targetLocal, DateTime nowLocal)
    {
        int days = (targetLocal.Date - nowLocal.Date).Days;
        if (Tr)
        {
            var day = days switch
            {
                0 => "",
                1 => "yarın ",
                _ => targetLocal.ToString("d MMMM ", TrCulture),
            };
            return $"{day}{TimeOfDay(targetLocal)}'{LocativeSuffix(targetLocal)}";
        }
        return days switch
        {
            0 => $"at {TimeOfDay(targetLocal)}",
            1 => $"tomorrow at {TimeOfDay(targetLocal)}",
            _ => $"on {targetLocal.ToString("MMM d", EnCulture)} at {TimeOfDay(targetLocal)}",
        };
    }

    /// <summary>Suffix as the time is read aloud: 23:45 "kırk beşte", 07:30 "otuzda".</summary>
    internal static string LocativeSuffix(DateTime t)
    {
        // The last spoken word decides vowel harmony and consonant assimilation.
        int n = t.Minute != 0 ? t.Minute : t.Hour;
        string word = LastWord(n);
        char lastVowel = word.Last(c => "aeıioöuü".Contains(c));
        bool back = "aıou".Contains(lastVowel);
        bool hard = "çfhkpsşt".Contains(word[^1]);
        return (hard ? "t" : "d") + (back ? "a" : "e");
    }

    private static string LastWord(int n)
    {
        string[] ones = ["sıfır", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz"];
        string[] tens = ["", "on", "yirmi", "otuz", "kırk", "elli"];
        if (n == 0) return "sıfır";
        return n % 10 != 0 ? ones[n % 10] : tens[n / 10];
    }
}
