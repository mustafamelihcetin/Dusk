using System.Globalization;
using Dusk.Core;

namespace Dusk.Tests;

/// <summary>Loc and TimeText are static; tests that switch language must not run in parallel.</summary>
[CollectionDefinition("Loc", DisableParallelization = true)]
public class LocCollection;

[Collection("Loc")]
public class LocTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 21, 20, 0, 0);

    public LocTests() => TimeText.EnCulture = CultureInfo.GetCultureInfo("en-US");
    public void Dispose() => Loc.Current = Lang.Tr;

    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 h")]
    [InlineData(135, "2 h 15 min")]
    public void English_duration(int minutes, string expected)
    {
        Loc.Current = Lang.En;
        Assert.Equal(expected, TimeText.Duration(minutes));
    }

    [Fact]
    public void English_times_and_sentences()
    {
        Loc.Current = Lang.En;
        Assert.Equal("at 11:45 PM", TimeText.At(Now.Date.AddHours(23).AddMinutes(45), Now));
        Assert.Equal("tomorrow at 1:30 AM", TimeText.At(Now.Date.AddDays(1).AddHours(1).AddMinutes(30), Now));
        Assert.Equal("Shuts down at 11:45 PM", Loc.HappensAt(PowerAction.Shutdown, "at 11:45 PM"));
        Assert.Equal("Sleeps in 30 min", Loc.HappensIn(PowerAction.Sleep, "30 min"));
        Assert.Equal("Restart in 1 h", Loc.StartButton(PowerAction.Restart, "1 h"));
        Assert.Equal("You'll be signed out soon", Loc.WarningHeadline(PowerAction.SignOut));
    }

    [Fact]
    public void Turkish_sentences()
    {
        Loc.Current = Lang.Tr;
        Assert.Equal("23:45'te kapanacak", Loc.HappensAt(PowerAction.Shutdown, "23:45'te"));
        Assert.Equal("30 dk sonra uyku moduna geçecek", Loc.HappensIn(PowerAction.Sleep, "30 dk"));
        Assert.Equal("Yeniden başlat · 1 sa sonra", Loc.StartButton(PowerAction.Restart, "1 sa"));
        Assert.Equal("Bilgisayar birazdan kapanacak", Loc.WarningHeadline(PowerAction.Shutdown));
    }

    [Theory]
    [InlineData(Lang.Tr)]
    [InlineData(Lang.En)]
    public void Every_action_has_distinct_texts(Lang lang)
    {
        Loc.Current = lang;
        var actions = Enum.GetValues<PowerAction>();
        foreach (var f in new Func<PowerAction, string>[] { Loc.Label, Loc.Now, Loc.WarningHeadline, a => Loc.HappensAt(a, "x") })
        {
            var texts = actions.Select(f).ToList();
            Assert.All(texts, t => Assert.False(string.IsNullOrWhiteSpace(t)));
            Assert.Equal(texts.Count, texts.Distinct().Count());
        }
    }

    [Fact]
    public void English_has_no_turkish_letters()
    {
        Loc.Current = Lang.En;
        var all = typeof(Loc).GetProperties().Where(p => p.PropertyType == typeof(string) && p.GetMethod!.IsStatic)
            .Select(p => (string)p.GetValue(null)!)
            .Concat(Enum.GetValues<PowerAction>().SelectMany(a => new[]
                { Loc.Label(a), Loc.Now(a), Loc.WarningHeadline(a), Loc.WarningDetail(a), Loc.HappensIn(a, "1 h") }))
            .Concat([Loc.Missed("1:00 AM"), Loc.DryRunDone(PowerAction.Shutdown), Loc.Failed("x"), Loc.Refused(5), Loc.Left("1:00")]);
        foreach (var s in all)
            Assert.DoesNotMatch("[çğıöşüÇĞİÖŞÜ]", s);
    }
}
