using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FiveMConfigEditorWPF.Models
{
    public class CacheFolderInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool Exists { get; set; }
        public long SizeBytes { get; set; }
        public int FileCount { get; set; }
        public string FormattedSize => FiveMCacheHelper.FormatBytes(SizeBytes);
    }

    public static class FiveMCacheHelper
    {
        private static readonly (string Name, string Description)[] CacheDefinitions =
        {
            ("cache", "Cache umum & shaders"),
            ("server-cache", "Cache resource server publik"),
            ("server-cache-priv", "Cache privat / asset terenkripsi server"),
            ("nui-storage", "Browser NUI & LocalStorage per server")
        };

        public static List<CacheFolderInfo> GetCacheFolders(string fiveMAppDir)
        {
            var result = new List<CacheFolderInfo>(CacheDefinitions.Length);
            var hasBase = !string.IsNullOrWhiteSpace(fiveMAppDir);

            foreach (var (name, desc) in CacheDefinitions)
            {
                var fullPath = hasBase ? Path.Combine(fiveMAppDir, "data", name) : "";
                result.Add(new CacheFolderInfo
                {
                    Name = name,
                    Description = desc,
                    FullPath = fullPath,
                    Exists = hasBase && Directory.Exists(fullPath)
                });
            }

            return result;
        }

        public static Task<List<CacheFolderInfo>> ScanCacheFoldersAsync(string fiveMAppDir, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                var folders = GetCacheFolders(fiveMAppDir);
                foreach (var folder in folders)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!folder.Exists) continue;

                    var (size, count) = MeasureDirectory(folder.FullPath, ct);
                    folder.SizeBytes = size;
                    folder.FileCount = count;
                }
                return folders;
            }, ct);
        }

        public static Task<(bool Ok, long FreedBytes, string Message)> ClearCacheFolderAsync(string folderPath)
        {
            return Task.Run(() =>
            {
                if (FiveMLauncher.IsFiveMRunning())
                    return (false, 0L, "Tutup FiveM terlebih dahulu sebelum membersihkan cache.");

                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                    return (false, 0L, "Folder cache tidak ditemukan.");

                long freed = 0;
                int deletedFiles = 0;
                int skippedFiles = 0;

                DeleteContents(folderPath, ref freed, ref deletedFiles, ref skippedFiles);

                var msg = skippedFiles == 0
                    ? $"Berhasil menghapus {deletedFiles} file ({FormatBytes(freed)} dibebaskan)."
                    : $"Berhasil menghapus {deletedFiles} file ({FormatBytes(freed)} dibebaskan), {skippedFiles} file dilewati karena terkunci.";

                return (true, freed, msg);
            });
        }

        public static async Task<(bool Ok, string Message)> InstallCachePackageAsync(
            string archiveOrExePath,
            string fiveMAppDir,
            IProgress<string>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(archiveOrExePath) || !File.Exists(archiveOrExePath))
                return (false, "File paket cache tidak ditemukan.");

            var ext = Path.GetExtension(archiveOrExePath).ToLowerInvariant();

            if (ext == ".zip")
            {
                if (FiveMLauncher.IsFiveMRunning())
                    return (false, "Tutup FiveM terlebih dahulu sebelum memasang paket cache.");

                if (string.IsNullOrWhiteSpace(fiveMAppDir))
                    return (false, "Direktori FiveM.app belum dikonfigurasi.");

                var targetDir = Path.Combine(fiveMAppDir, "data", "server-cache-priv");

                try
                {
                    progress?.Report("Menyiapkan folder server-cache-priv...");
                    await Task.Run(() =>
                    {
                        Directory.CreateDirectory(targetDir);
                        progress?.Report($"Mengekstrak {Path.GetFileName(archiveOrExePath)}...");
                        ZipFile.ExtractToDirectory(archiveOrExePath, targetDir, overwriteFiles: true);
                    });

                    progress?.Report("Ekstraksi selesai.");
                    return (true, $"Paket cache berhasil diekstrak ke {targetDir}.");
                }
                catch (Exception ex)
                {
                    return (false, $"Gagal mengekstrak paket cache: {ex.Message}");
                }
            }

            if (ext == ".exe")
            {
                try
                {
                    progress?.Report($"Menjalankan installer {Path.GetFileName(archiveOrExePath)}...");
                    Process.Start(new ProcessStartInfo(archiveOrExePath)
                    {
                        UseShellExecute = true
                    });
                    return (true, $"Installer {Path.GetFileName(archiveOrExePath)} dijalankan.");
                }
                catch (Exception ex)
                {
                    return (false, $"Gagal menjalankan installer: {ex.Message}");
                }
            }

            return (false, "Format file tidak didukung. Gunakan file .zip atau .exe.");
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unitIndex = 0;
            while (value >= 1024 && unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }
            return unitIndex == 0
                ? $"{bytes} B"
                : $"{value:0.##} {units[unitIndex]}";
        }

        private static (long SizeBytes, int FileCount) MeasureDirectory(string rootPath, CancellationToken ct)
        {
            long totalBytes = 0;
            int fileCount = 0;
            var stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var dir = stack.Pop();

                try
                {
                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            totalBytes += new FileInfo(file).Length;
                            fileCount++;
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException) { }

                try
                {
                    foreach (var subDir in Directory.EnumerateDirectories(dir))
                    {
                        stack.Push(subDir);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException) { }
            }

            return (totalBytes, fileCount);
        }

        private static void DeleteContents(string dirPath, ref long freedBytes, ref int deletedFiles, ref int skippedFiles)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(dirPath))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.IsReadOnly) info.IsReadOnly = false;
                        var len = info.Length;
                        info.Delete();
                        freedBytes += len;
                        deletedFiles++;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        skippedFiles++;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException) { }

            try
            {
                foreach (var subDir in Directory.EnumerateDirectories(dirPath))
                {
                    DeleteContents(subDir, ref freedBytes, ref deletedFiles, ref skippedFiles);
                    try
                    {
                        Directory.Delete(subDir, recursive: false);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException) { }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException) { }
        }
    }
}
