using System.Globalization;

namespace Dusk.Core;

public static class TimeText
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>A chosen duration in words: "45 dk", "1 sa", "2 sa 15 dk".</summary>
    public static string Duration(int minutes)
    {
        if (minutes <= 0) return "0 dk";
        int h = minutes / 60, m = minutes % 60;
        if (h == 0) return $"{m} dk";
        return m == 0 ? $"{h} sa" : $"{h} sa {m} dk";
    }

    /// <summary>A live countdown: "4:59", "1:04:59".</summary>
    public static string Clock(TimeSpan t)
    {
        // Round up so the display reaches 0:00 exactly when the action fires.
        var s = (long)Math.Ceiling(Math.Max(0, t.TotalSeconds));
        long h = s / 3600, m = s % 3600 / 60, sec = s % 60;
        return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m}:{sec:00}";
    }

    /// <summary>Wall-clock time of day: "23:45".</summary>
    public static string TimeOfDay(DateTime local) => local.ToString("HH:mm", Tr);

    /// <summary>"23:45'te", "yarın 07:30'da", with the correct Turkish suffix.</summary>
    public static string At(DateTime targetLocal, DateTime nowLocal)
    {
        var day = (targetLocal.Date - nowLocal.Date).Days switch
        {
            0 => "",
            1 => "yarın ",
            _ => targetLocal.ToString("d MMMM ", Tr),
        };
        return $"{day}{TimeOfDay(targetLocal)}'{LocativeSuffix(targetLocal)}";
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
