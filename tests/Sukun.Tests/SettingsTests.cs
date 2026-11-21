using Sukun.Core;

namespace Sukun.Tests;

public class SettingsTests
{
    [Fact]
    public void Round_trips_and_survives_garbage()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "a", "ayarlar.json");
            var s = new Settings { Action = PowerAction.Sleep, Minutes = 95, Force = true, ShowClockTime = true };
            s.Save(path);
            Assert.Equal(s, Settings.Load(path));

            File.WriteAllText(path, "{ bozuk");
            Assert.Equal(new Settings(), Settings.Load(path));

            File.WriteAllText(path, """{ "Minutes": 99999, "Action": 42 }""");
            var n = Settings.Load(path);
            Assert.Equal(Settings.MaxMinutes, n.Minutes);
            Assert.Equal(PowerAction.Shutdown, n.Action);
        }
        finally
        {
            dir.Delete(true);
        }
    }
}
