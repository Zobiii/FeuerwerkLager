using System.Text;

namespace FeuerwerkLager.Logs;

public static class PlainFileLogger
{
    private static readonly object _lock = new();
    private static readonly string LogFilePath = Path.Combine("Logs", "feuerwerk.log");

    static PlainFileLogger()
    {
        Directory.CreateDirectory("Logs");
    }

    public static void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
        lock (_lock)
        {
            File.AppendAllText(LogFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    public static string[] ReadAllLines()
    {
        if (!File.Exists(LogFilePath))
            return Array.Empty<string>();

        return File.ReadAllLines(LogFilePath, Encoding.UTF8);
    }

    public static void Clear()
    {
        if (File.Exists(LogFilePath))
            File.Delete(LogFilePath);
    }
}
