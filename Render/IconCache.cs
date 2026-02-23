using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace league_mastery_overlay.Render;

/// <summary>
/// Downloads mastery icon PNGs from CommunityDragon on first run and caches them in
/// %APPDATA%\LeagueMasteryOverlay\icons\.  Subsequent launches load from disk with no
/// network traffic unless a file is missing.
/// </summary>
public static class IconCache
{
    // ── CDN base ─────────────────────────────────────────────────────────────

    private const string CdnBase =
        "https://raw.communitydragon.org/latest/plugins/rcp-fe-lol-collections/global/default/images/item-element/";

    // ── Local cache root ─────────────────────────────────────────────────────

    public static readonly string CacheDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LeagueMasteryOverlay",
            "icons");

    // ── File manifests ───────────────────────────────────────────────────────

    /// <summary>Modern crest icons: levels 0–10.</summary>
    public static IEnumerable<string> ModernFileNames =>
        Enumerable.Range(0, 11).Select(i => $"crest-and-banner-mastery-{i}.png");

    /// <summary>Legacy icons: levels 0–7.</summary>
    public static IEnumerable<string> LegacyFileNames =>
        Enumerable.Range(0, 8).Select(i => $"mastery-{i}.png");

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the local cache path for a given filename, or null if the file
    /// is not yet cached.
    /// </summary>
    public static string? GetCachedPath(string fileName)
    {
        var path = Path.Combine(CacheDir, fileName);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Downloads all missing icons for both sets in the background.
    /// Safe to call multiple times — already-cached files are skipped.
    /// Exceptions are logged but never propagated.
    /// </summary>
    public static async Task EnsureAllCachedAsync()
    {
        var allFiles = ModernFileNames.Concat(LegacyFileNames).ToList();
        var missing = allFiles.Where(f => GetCachedPath(f) == null).ToList();

        if (missing.Count == 0)
        {
            Debug.WriteLine("[IconCache] All icons already cached.");
            return;
        }

        Debug.WriteLine($"[IconCache] Downloading {missing.Count} missing icon(s)…");

        try
        {
            Directory.CreateDirectory(CacheDir);

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(30);

            // Download concurrently, up to 4 at a time.
            var semaphore = new SemaphoreSlim(4);
            var tasks = missing.Select(async fileName =>
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    await DownloadFileAsync(http, fileName).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            Debug.WriteLine("[IconCache] All icons downloaded.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IconCache] Download batch failed: {ex.Message}");
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task DownloadFileAsync(HttpClient http, string fileName)
    {
        var destPath = Path.Combine(CacheDir, fileName);

        // Double-check inside the task in case a parallel task already wrote it.
        if (File.Exists(destPath))
            return;

        var url = CdnBase + fileName;
        try
        {
            var bytes = await http.GetByteArrayAsync(url).ConfigureAwait(false);
            // Write to a temp file first so a partial download never leaves a corrupt cache entry.
            var tmp = destPath + ".tmp";
            await File.WriteAllBytesAsync(tmp, bytes).ConfigureAwait(false);
            File.Move(tmp, destPath, overwrite: true);
            Debug.WriteLine($"[IconCache] Cached {fileName}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IconCache] Failed to download {fileName}: {ex.Message}");
        }
    }
}
