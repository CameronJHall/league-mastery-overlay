using System.Diagnostics;
using System.IO;

namespace league_mastery_overlay.League;

internal sealed class LcuAuthProvider
{
    public bool TryGetAuth(out LcuAuthInfo info)
    {
        info = null!;

        try
        {
            var process = Process.GetProcessesByName("LeagueClientUx")
                .FirstOrDefault();
            if (process == null)
                return false;

            string lockfilePath = Path.Combine(
                Path.GetDirectoryName(process.MainModule!.FileName)!,
                "lockfile"
            );

            if (!File.Exists(lockfilePath))
                return false;

            using var stream = new FileStream(
                lockfilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            var parts = content.Split(':');

            info = new LcuAuthInfo(
                Port: int.Parse(parts[2]),
                Password: parts[3]
            );
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}