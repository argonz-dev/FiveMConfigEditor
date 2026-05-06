using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class ModPackMetadata
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<string> ModFiles { get; set; } = new();
        public List<string> PluginFiles { get; set; } = new();
        public long TotalSizeBytes { get; set; } = 0;
    }

    public static class ModPackManager
    {
        /// <summary>Export active mods and plugins to a .fmpack file</summary>
        public static void ExportPack(string outputPath, string packName, string description, string author)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"fmpack_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var modsDir = Path.Combine(tempDir, "mods");
                var pluginsDir = Path.Combine(tempDir, "plugins");
                Directory.CreateDirectory(modsDir);
                Directory.CreateDirectory(pluginsDir);

                var metadata = new ModPackMetadata
                {
                    Name = packName,
                    Description = description,
                    Author = author,
                    CreatedAt = DateTime.Now
                };

                long totalSize = 0;

                // Copy active mods
                if (Directory.Exists(ModManager.ModsDir))
                {
                    foreach (var file in Directory.GetFiles(ModManager.ModsDir, "*.rpf"))
                    {
                        var dest = Path.Combine(modsDir, Path.GetFileName(file));
                        File.Copy(file, dest, true);
                        metadata.ModFiles.Add(Path.GetFileName(file));
                        totalSize += new FileInfo(file).Length;
                    }

                    foreach (var dir in Directory.GetDirectories(ModManager.ModsDir))
                    {
                        var dirName = Path.GetFileName(dir);
                        var dest = Path.Combine(modsDir, dirName);
                        CopyDirectory(dir, dest);
                        metadata.ModFiles.Add(dirName + "/");
                        totalSize += GetDirectorySize(dir);
                    }
                }

                // Copy active plugins
                if (Directory.Exists(ModManager.PluginsDir))
                {
                    foreach (var file in Directory.GetFiles(ModManager.PluginsDir))
                    {
                        var fileName = Path.GetFileName(file);
                        // Skip non-plugin files
                        if (!IsPluginFile(fileName)) continue;

                        var dest = Path.Combine(pluginsDir, fileName);
                        File.Copy(file, dest, true);
                        metadata.PluginFiles.Add(fileName);
                        totalSize += new FileInfo(file).Length;
                    }

                    foreach (var dir in Directory.GetDirectories(ModManager.PluginsDir))
                    {
                        var dirName = Path.GetFileName(dir);
                        var dest = Path.Combine(pluginsDir, dirName);
                        CopyDirectory(dir, dest);
                        metadata.PluginFiles.Add(dirName + "/");
                        totalSize += GetDirectorySize(dir);
                    }
                }

                metadata.TotalSizeBytes = totalSize;

                // Save metadata
                var metadataPath = Path.Combine(tempDir, "pack.json");
                File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

                // Create ZIP
                if (File.Exists(outputPath)) File.Delete(outputPath);
                ZipFile.CreateFromDirectory(tempDir, outputPath, CompressionLevel.Optimal, false);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>Import a .fmpack file and extract to mods/plugins folders</summary>
        public static ModPackMetadata ImportPack(string packPath, bool overwrite = false)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"fmpack_import_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract ZIP
                ZipFile.ExtractToDirectory(packPath, tempDir);

                // Read metadata
                var metadataPath = Path.Combine(tempDir, "pack.json");
                if (!File.Exists(metadataPath))
                    throw new Exception("Invalid mod pack: metadata not found.");

                var metadata = JsonSerializer.Deserialize<ModPackMetadata>(File.ReadAllText(metadataPath));
                if (metadata == null)
                    throw new Exception("Invalid mod pack: failed to parse metadata.");

                // Ensure target directories exist
                ModManager.EnsureDir(ModManager.ModsDir);
                ModManager.EnsureDir(ModManager.PluginsDir);

                // Copy mods
                var modsSource = Path.Combine(tempDir, "mods");
                if (Directory.Exists(modsSource))
                {
                    foreach (var file in Directory.GetFiles(modsSource))
                    {
                        var fileName = Path.GetFileName(file);
                        var dest = Path.Combine(ModManager.ModsDir, fileName);
                        if (!overwrite && File.Exists(dest))
                            throw new Exception($"File already exists: {fileName}. Enable overwrite to replace.");
                        File.Copy(file, dest, overwrite);
                    }

                    foreach (var dir in Directory.GetDirectories(modsSource))
                    {
                        var dirName = Path.GetFileName(dir);
                        var dest = Path.Combine(ModManager.ModsDir, dirName);
                        if (!overwrite && Directory.Exists(dest))
                            throw new Exception($"Folder already exists: {dirName}. Enable overwrite to replace.");
                        if (Directory.Exists(dest)) Directory.Delete(dest, true);
                        CopyDirectory(dir, dest);
                    }
                }

                // Copy plugins
                var pluginsSource = Path.Combine(tempDir, "plugins");
                if (Directory.Exists(pluginsSource))
                {
                    foreach (var file in Directory.GetFiles(pluginsSource))
                    {
                        var fileName = Path.GetFileName(file);
                        var dest = Path.Combine(ModManager.PluginsDir, fileName);
                        if (!overwrite && File.Exists(dest))
                            throw new Exception($"File already exists: {fileName}. Enable overwrite to replace.");
                        File.Copy(file, dest, overwrite);
                    }

                    foreach (var dir in Directory.GetDirectories(pluginsSource))
                    {
                        var dirName = Path.GetFileName(dir);
                        var dest = Path.Combine(ModManager.PluginsDir, dirName);
                        if (!overwrite && Directory.Exists(dest))
                            throw new Exception($"Folder already exists: {dirName}. Enable overwrite to replace.");
                        if (Directory.Exists(dest)) Directory.Delete(dest, true);
                        CopyDirectory(dir, dest);
                    }
                }

                return metadata;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        /// <summary>Read metadata from a .fmpack file without extracting</summary>
        public static ModPackMetadata? ReadPackMetadata(string packPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(packPath);
                var metadataEntry = archive.GetEntry("pack.json");
                if (metadataEntry == null) return null;

                using var stream = metadataEntry.Open();
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<ModPackMetadata>(json);
            }
            catch { return null; }
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(dir, destSubDir);
            }
        }

        private static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long total = 0;
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                try { total += new FileInfo(file).Length; } catch { }
            return total;
        }

        private static bool IsPluginFile(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext == ".asi" || ext == ".dll" || ext == ".addon";
        }
    }
}
