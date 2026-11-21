using System.Text;
using System.Text.Json;

namespace HuntKdaCalculator;

/// <summary>Состояние приложения + надёжное сохранение в %APPDATA%\HuntKdaCalculator\state.json.</summary>
sealed class AppState
{
    const int Max = 999_999;

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public bool TopMost { get; set; }
    public int? WindowX { get; set; }
    public int? WindowY { get; set; }

    public static string Dir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HuntKdaCalculator");

    static string FilePath => Path.Combine(Dir, "state.json");
    static readonly object Gate = new();
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static AppState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppState>(File.ReadAllText(FilePath, Encoding.UTF8));
                if (s != null)
                {
                    s.Kills = Math.Clamp(s.Kills, 0, Max);
                    s.Deaths = Math.Clamp(s.Deaths, 0, Max);
                    s.Assists = Math.Clamp(s.Assists, 0, Max);
                    return s;
                }
            }
        }
        catch { /* повреждённый файл -> старт с нулей */ }
        return new AppState();
    }

    /// <summary>Атомарная запись: temp-файл -> flush на диск -> замена основного файла.</summary>
    public void Save()
    {
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var tmp = FilePath + ".tmp";
                var json = JsonSerializer.Serialize(this, JsonOpts);

                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var w = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    w.Write(json);
                    w.Flush();
                    fs.Flush(true); // принудительно сбросить на физический диск
                }

                if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
                else File.Move(tmp, FilePath);
            }
            catch { /* не роняем приложение из-за ошибки диска */ }
        }
    }
}
