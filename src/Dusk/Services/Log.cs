using System.IO;

namespace Dusk.Services;

/// <summary>
/// A short, human-readable event log (%AppData%\Dusk\dusk.log) so "why didn't
/// it shut down last night?" always has an answer. Kept under ~256 KB.
/// </summary>
public static class Log
{
    private static readonly object Gate = new();

    public static string PathName => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dusk", "dusk.log");

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName)!);
                var fi = new FileInfo(PathName);
                if (fi.Exists && fi.Length > 256 * 1024)
                    File.Move(PathName, PathName + ".1", overwrite: true);
                File.AppendAllText(PathName, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
