using Dusk.Core;

namespace Dusk.Tests;

[Collection("Loc")]
public class TimeTextTests
{
    public TimeTextTests() => Loc.Current = Lang.Tr;

    [Theory]
    [InlineData(45, "45 dk")]
    [InlineData(60, "1 sa")]
    [InlineData(135, "2 sa 15 dk")]
    [InlineData(0, "0 dk")]
    public void Duration(int minutes, string expected) => Assert.Equal(expected, TimeText.Duration(minutes));

    [Theory]
    [InlineData(299.2, "5:00")]
    [InlineData(59, "0:59")]
    [InlineData(3899, "1:04:59")]
    [InlineData(0, "0:00")]
    [InlineData(-3, "0:00")]
    public void Clock(double seconds, string expected) =>
        Assert.Equal(expected, TimeText.Clock(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(23, 45, "23:45'te")]   // kırk beş-te
    [InlineData(7, 30, "07:30'da")]    // otuz-da
    [InlineData(22, 0, "22:00'de")]    // yirmi iki-de
    [InlineData(19, 0, "19:00'da")]    // on dokuz-da
    [InlineData(1, 3, "01:03'te")]     // üç-te
    [InlineData(0, 40, "00:40'ta")]    // kırk-ta
    [InlineData(6, 0, "06:00'da")]     // altı-da
    [InlineData(4, 0, "04:00'te")]     // dört-te
    [InlineData(23, 50, "23:50'de")]   // elli-de
    [InlineData(5, 20, "05:20'de")]    // yirmi-de
    [InlineData(5, 10, "05:10'da")]    // on-da
    public void At_uses_correct_suffix(int h, int m, string expected)
    {
        var now = new DateTime(2026, 11, 21, 0, 0, 0);
        Assert.Equal(expected, TimeText.At(now.Date.AddHours(h).AddMinutes(m), now));
    }

    [Fact]
    public void At_mentions_tomorrow()
    {
        var now = new DateTime(2026, 11, 21, 23, 0, 0);
        Assert.Equal("yarın 01:30'da", TimeText.At(now.AddHours(2).AddMinutes(30), now));
    }
}
