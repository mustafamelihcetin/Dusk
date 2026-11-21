using System.Text.Json;

namespace Sukun.Core;

/// <summary>The few choices worth remembering between launches.</summary>
public sealed record Settings
{
    public PowerAction Action { get; init; } = PowerAction.Shutdown;
    public int Minutes { get; init; } = 30;
    /// <summary>Close apps even if they have unsaved work. Off by default on purpose.</summary>
    public bool Force { get; init; }
    /// <summary>Show the target as a time of day (23:45) instead of a duration.</summary>
    public bool ShowClockTime { get; init; }

    public const int MinMinutes = 1;
    public const int MaxMinutes = 12 * 60;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sukun", "ayarlar.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static Settings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new Settings();
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
            return s.Normalized();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return new Settings(); // A broken file must never stop the app from opening.
        }
    }

    public void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Json));
            File.Move(tmp, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Settings are a convenience; failing to save must not interrupt the user.
        }
    }

    public Settings Normalized() => this with
    {
        Minutes = Math.Clamp(Minutes, MinMinutes, MaxMinutes),
        Action = Enum.IsDefined(Action) ? Action : PowerAction.Shutdown,
    };
}
