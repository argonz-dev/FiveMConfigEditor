using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiveMConfigEditorWPF.Models
{
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    public static class AiExecutionEngine
    {
        public static async Task<ExecutionResult> ExecutePowerShellAsync(string script)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    using var process = new Process { StartInfo = psi };
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null) outputBuilder.AppendLine(e.Data);
                    };
                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null) errorBuilder.AppendLine(e.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    bool exited = process.WaitForExit(45000); // 45s timeout
                    if (!exited)
                    {
                        try { process.Kill(); } catch { }
                        return new ExecutionResult
                        {
                            Success = false,
                            ExitCode = -1,
                            Output = outputBuilder.ToString(),
                            ErrorMessage = "Execution timed out (45 detik)."
                        };
                    }

                    string stdout = outputBuilder.ToString().Trim();
                    string stderr = errorBuilder.ToString().Trim();

                    bool success = process.ExitCode == 0;
                    string combined = stdout;
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        combined = string.IsNullOrEmpty(combined) ? stderr : $"{combined}\nError:\n{stderr}";
                    }

                    if (string.IsNullOrEmpty(combined))
                    {
                        combined = "(Perintah selesai dieksekusi tanpa output)";
                    }

                    return new ExecutionResult
                    {
                        Success = success,
                        ExitCode = process.ExitCode,
                        Output = combined,
                        ErrorMessage = stderr
                    };
                }
                catch (Exception ex)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        ExitCode = -1,
                        Output = "",
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        public static string ResolveFiveMAppPath()
        {
            if (!string.IsNullOrEmpty(AppState.FiveMPath) && Directory.Exists(AppState.FiveMPath))
                return AppState.FiveMPath;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string defaultPath = Path.Combine(localAppData, "FiveM", "FiveM.app");
            if (Directory.Exists(defaultPath)) return defaultPath;

            string dDrive = @"D:\FiveM\FiveM.app";
            if (Directory.Exists(dDrive)) return dDrive;

            return AppState.FiveMPath;
        }

        public static async Task<ExecutionResult> CleanFiveMCacheAsync()
        {
            return await Task.Run(() =>
            {
                if (GtaGraphicsHelper.IsGameRunning())
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        ExitCode = 1,
                        Output = "Peringatan: FiveM atau GTA V sedang berjalan!\nTutup FiveM terlebih dahulu sebelum membersihkan cache agar file tidak terkunci.",
                        ErrorMessage = "Game is running"
                    };
                }

                string appPath = ResolveFiveMAppPath();
                if (!Directory.Exists(appPath))
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        ExitCode = 1,
                        Output = $"Folder FiveM.app tidak ditemukan di: {appPath}",
                        ErrorMessage = "FiveM path not found"
                    };
                }

                string dataDir = Path.Combine(appPath, "data");
                var cacheTargets = new[]
                {
                    Path.Combine(dataDir, "cache"),
                    Path.Combine(dataDir, "server-cache"),
                    Path.Combine(dataDir, "server-cache-priv"),
                    Path.Combine(dataDir, "nui-storage")
                };

                int deletedFiles = 0;
                long deletedBytes = 0;
                var logs = new StringBuilder();
                logs.AppendLine($"[FiveM Cache Cleaner] Target folder: {dataDir}");

                foreach (var dir in cacheTargets)
                {
                    if (Directory.Exists(dir))
                    {
                        try
                        {
                            var di = new DirectoryInfo(dir);
                            foreach (var file in di.GetFiles("*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    deletedBytes += file.Length;
                                    file.Delete();
                                    deletedFiles++;
                                }
                                catch { }
                            }

                            foreach (var subDir in di.GetDirectories())
                            {
                                try { subDir.Delete(true); } catch { }
                            }

                            logs.AppendLine($"✓ Berhasil dibersihkan: {Path.GetFileName(dir)}");
                        }
                        catch (Exception ex)
                        {
                            logs.AppendLine($"! Gagal membersihkan {Path.GetFileName(dir)}: {ex.Message}");
                        }
                    }
                    else
                    {
                        logs.AppendLine($"- Folder tidak ada (sudah bersih): {Path.GetFileName(dir)}");
                    }
                }

                // Jangan hapus folder 'game-storage' agar player tidak download ulang base GTA
                logs.AppendLine("ℹ Direktori game-storage dilewati untuk mencegah download ulang aset.");
                double mb = deletedBytes / (1024.0 * 1024.0);
                logs.AppendLine($"\nRingkasan: {deletedFiles} file dihapus ({mb:F2} MB ruang disk dibebaskan).");

                return new ExecutionResult
                {
                    Success = true,
                    ExitCode = 0,
                    Output = logs.ToString(),
                    ErrorMessage = ""
                };
            });
        }

        public static async Task<ExecutionResult> ApplyGraphicsPresetAsync(string presetName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (GtaGraphicsHelper.IsGameRunning())
                    {
                        return new ExecutionResult
                        {
                            Success = false,
                            ExitCode = 1,
                            Output = "Peringatan: FiveM atau GTA V sedang berjalan!\nTutup game terlebih dahulu agar gta5_settings.xml dapat disimpan permanen.",
                            ErrorMessage = "Game running"
                        };
                    }

                    string path = GtaGraphicsHelper.GetDefaultPath();
                    if (!File.Exists(path))
                    {
                        string rockstar = GtaGraphicsHelper.GetRockstarPath();
                        if (File.Exists(rockstar)) path = rockstar;
                        else
                        {
                            return new ExecutionResult
                            {
                                Success = false,
                                ExitCode = 1,
                                Output = $"File gta5_settings.xml tidak ditemukan di {path}",
                                ErrorMessage = "Settings file not found"
                            };
                        }
                    }

                    // Backup first
                    var backupMsg = SettingsBackup.CreateBackup();
                    var data = GtaGraphicsHelper.Load(path);
                    bool isNvidia = GtaGraphicsHelper.IsNvidiaGpu();

                    string p = presetName.ToLowerInvariant();
                    if (p.Contains("comp") || p.Contains("fps") || p.Contains("rata_kiri") || p.Contains("low"))
                    {
                        data.TextureQuality = 1;       // High (clear textures, low VRAM impact)
                        data.ShaderQuality = 1;        // High
                        data.ShadowQuality = 1;        // Normal (Huge FPS gain)
                        data.ReflectionQuality = 1;    // High
                        data.WaterQuality = 0;         // Normal
                        data.ParticleQuality = 0;      // Normal
                        data.GrassQuality = 0;         // Normal (Stops grass stutter)
                        data.PostFX = 0;               // Normal
                        data.MSAA = 0;                 // Off
                        data.AnisotropicFiltering = 4;
                        data.Tessellation = 0;         // Off
                        data.Shadow_SoftShadows = 1;   // Soft
                        data.Shadow_LongShadows = false;
                        data.Shadow_ParticleShadows = false;
                        data.SSAO = 0;                 // Off
                        data.DoF = false;
                        data.LodScale = 0.7f;
                        data.CityDensity = 0.5f;
                        data.FXAA_Enabled = true;
                        data.TXAA_Enabled = false;
                    }
                    else if (p.Contains("ultra"))
                    {
                        data.TextureQuality = 2;
                        data.ShaderQuality = 2;
                        data.ShadowQuality = 3;
                        data.ReflectionQuality = 3;
                        data.WaterQuality = 2;
                        data.ParticleQuality = 2;
                        data.GrassQuality = 3;
                        data.PostFX = 3;
                        data.MSAA = 4;
                        data.AnisotropicFiltering = 16;
                        data.Tessellation = 3;
                        data.Shadow_SoftShadows = isNvidia ? 5 : 3;
                        data.Shadow_LongShadows = true;
                        data.Shadow_ParticleShadows = true;
                        data.SSAO = 2;
                        data.DoF = true;
                        data.LodScale = 1.0f;
                        data.CityDensity = 1.0f;
                    }
                    else // RTX 5060 Ti / Balanced / High
                    {
                        data.TextureQuality = 2;       // Very High
                        data.ShaderQuality = 2;        // Very High
                        data.ShadowQuality = 2;        // High
                        data.ReflectionQuality = 2;    // Very High
                        data.WaterQuality = 1;         // High
                        data.ParticleQuality = 1;      // High
                        data.GrassQuality = 1;         // High
                        data.PostFX = 2;               // High
                        data.MSAA = 0;                 // Off (gunakan FXAA/TXAA agar 144+ FPS)
                        data.AnisotropicFiltering = 16;
                        data.Tessellation = 2;         // High
                        data.Shadow_SoftShadows = isNvidia ? 5 : 3; // NVIDIA PCSS
                        data.Shadow_LongShadows = true;
                        data.Shadow_ParticleShadows = true;
                        data.SSAO = 2;
                        data.DoF = false;
                        data.LodScale = 1.0f;
                        data.CityDensity = 1.0f;
                        data.FXAA_Enabled = true;
                        data.TXAA_Enabled = true;
                    }

                    GtaGraphicsHelper.Sanitize(data);
                    GtaGraphicsHelper.Save(path, data);

                    return new ExecutionResult
                    {
                        Success = true,
                        ExitCode = 0,
                        Output = $"✓ Preset grafis '{presetName}' berhasil diterapkan ke gta5_settings.xml!\n" +
                                 $"{backupMsg}\n" +
                                 $"Pengaturan kunci: Texture={data.TextureQuality}, Shadow={data.ShadowQuality}, Grass={data.GrassQuality}, MSAA={data.MSAA}, LodScale={data.LodScale}.",
                        ErrorMessage = ""
                    };
                }
                catch (Exception ex)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        ExitCode = 1,
                        Output = $"Gagal menyimpan pengaturan: {ex.Message}",
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        public static async Task<ExecutionResult> OptimizeCitizenFxIniAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    string iniPath = AppState.IniPath;
                    if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath))
                    {
                        string appPath = ResolveFiveMAppPath();
                        iniPath = Path.Combine(appPath, "CitizenFX.ini");
                    }

                    if (!File.Exists(iniPath))
                    {
                        return new ExecutionResult
                        {
                            Success = false,
                            ExitCode = 1,
                            Output = $"CitizenFX.ini tidak ditemukan di: {iniPath}",
                            ErrorMessage = "File not found"
                        };
                    }

                    var data = IniHelper.Load(iniPath);
                    if (data.PoolSizes == null)
                    {
                        data.PoolSizes = new System.Collections.Generic.Dictionary<string, int>();
                    }

                    data.PoolSizes["rage::fwArchetypeDef"] = 256000;
                    data.PoolSizes["CBaseModelInfo"] = 128000;
                    data.PoolSizes["rage::phBoundComposite"] = 64000;

                    IniHelper.Save(iniPath, data);

                    return new ExecutionResult
                    {
                        Success = true,
                        ExitCode = 0,
                        Output = $"✓ CitizenFX.ini berhasil dioptimasi dengan pool memory baru:\n" +
                                 $"  - rage::fwArchetypeDef = 256000\n" +
                                 $"  - CBaseModelInfo = 128000\n" +
                                 $"  - rage::phBoundComposite = 64000\n" +
                                 $"Mencegah crash MLO/custom maps & stutter texture memory.",
                        ErrorMessage = ""
                    };
                }
                catch (Exception ex)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        ExitCode = 1,
                        Output = $"Gagal optimasi CitizenFX.ini: {ex.Message}",
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        public static async Task<ExecutionResult> SetHighPerformancePowerPlanAsync()
        {
            string script = @"
$powerPlan = powercfg /getactivescheme
powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
ipconfig /flushdns
Write-Output 'High Performance Power Plan diaktifkan & DNS Cache berhasil di-flush.'
";
            return await ExecutePowerShellAsync(script);
        }
    }
}
