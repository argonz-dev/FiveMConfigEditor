using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FiveMConfigEditorWPF.Models
{
    public static class FiveMLauncher
    {
        public static bool IsFiveMRunning()
        {
            return Process.GetProcesses().Any(p =>
            {
                try
                {
                    return p.ProcessName.Equals("FiveM", StringComparison.OrdinalIgnoreCase)
                        || (p.ProcessName.StartsWith("FiveM_", StringComparison.OrdinalIgnoreCase)
                            && p.ProcessName.EndsWith("GTAProcess", StringComparison.OrdinalIgnoreCase));
                }
                catch { return false; }
                finally { p.Dispose(); }
            });
        }

        private static void KillRunningFiveMProcesses()
        {
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.Equals("FiveM", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.StartsWith("FiveM_", StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kill(entireProcessTree: true);
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }
        }

        public static void CloseRunningFiveM()
        {
            KillRunningFiveMProcesses();
            Thread.Sleep(800);
        }

        public static async Task CloseRunningFiveMAsync()
        {
            KillRunningFiveMProcesses();
            await Task.Delay(800);
        }

        private static string? TryGetParentDir(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                return Directory.GetParent(path.Trim())?.FullName;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetDirName(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                var dir = Path.GetDirectoryName(path.Trim());
                return string.IsNullOrWhiteSpace(dir) ? null : dir;
            }
            catch
            {
                return null;
            }
        }

        public static string? ResolveFiveMExe()
        {
            string? iniDir = TryGetDirName(AppState.IniPath);
            var candidates = new[]
            {
                TryGetParentDir(AppState.FiveMPath),
                string.IsNullOrWhiteSpace(AppState.FiveMPath) ? null : AppState.FiveMPath.Trim(),
                TryGetParentDir(iniDir),
                iniDir,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM")
            };

            foreach (var dir in candidates)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    var exe = Path.Combine(dir, "FiveM.exe");
                    if (File.Exists(exe)) return exe;
                }
                catch { }
            }
            return null;
        }

        private static bool HasUnsafeUriChars(string value)
        {
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c) || c == '"' || c == '\'' || char.IsControl(c))
                    return true;
            }
            return false;
        }

        public static string? NormalizeConnectUri(string? serverUrl)
        {
            if (string.IsNullOrWhiteSpace(serverUrl)) return null;
            var raw = serverUrl.Trim();
            if (HasUnsafeUriChars(raw)) return null;

            string uri;
            if (raw.StartsWith("fivem://", StringComparison.OrdinalIgnoreCase))
            {
                string rest = raw["fivem://".Length..].TrimStart('/');
                if (string.IsNullOrEmpty(rest)) return null;
                uri = raw;
            }
            else
            {
                if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    raw = raw["https://".Length..];
                else if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    raw = raw["http://".Length..];

                raw = raw.TrimStart('/');
                if (string.IsNullOrEmpty(raw)) return null;
                uri = $"fivem://connect/{raw}";
            }

            return !HasUnsafeUriChars(uri) && Uri.TryCreate(uri, UriKind.Absolute, out _) ? uri : null;
        }

        /// <summary>
        /// Launch FiveM via explorer.exe so Windows Shell becomes FiveM's parent process
        /// (avoids FiveM's "should be launched directly from the shell" / "no master process" error).
        /// </summary>
        public static (bool Ok, string Message) Launch(string? serverUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                var connectUri = NormalizeConnectUri(serverUrl);
                if (string.IsNullOrEmpty(connectUri) || HasUnsafeUriChars(connectUri))
                    return (false, "URL server tidak valid (tidak boleh mengandung spasi, tanda kutip, atau karakter kontrol).");

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = connectUri,
                        UseShellExecute = false
                    });
                    return (true, $"Membuka FiveM & connecting ke {connectUri}...");
                }
                catch (Exception ex)
                {
                    return (false, $"Gagal membuka FiveM: {ex.Message}");
                }
            }

            var exePath = ResolveFiveMExe();
            if (string.IsNullOrEmpty(exePath))
                return (false, "FiveM.exe tidak ditemukan. Pastikan FiveM Folder Path di menu Config sudah benar.");

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{exePath}\"",
                    UseShellExecute = false
                });
                return (true, $"Meluncurkan FiveM ({Path.GetFileName(exePath)})...");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal meluncurkan FiveM: {ex.Message}");
            }
        }
    }
}
