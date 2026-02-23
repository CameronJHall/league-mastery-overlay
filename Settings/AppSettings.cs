using System.IO;
using System.Text.Json;
using league_mastery_overlay.Render;

namespace league_mastery_overlay.Settings;

/// <summary>
/// Persists user preferences to %APPDATA%\LeagueMasteryOverlay\settings.json.
/// </summary>
public class AppSettings
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueMasteryOverlay");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // ── Persisted properties ──────────────────────────────────────────────────

    public MasteryIconSet IconSet { get; set; } = MasteryIconSet.Modern;

    // ── Load / Save ───────────────────────────────────────────────────────────

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Corrupt or unreadable — fall back to defaults silently.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best-effort: ignore save failures (e.g. read-only filesystem).
        }
    }
}
