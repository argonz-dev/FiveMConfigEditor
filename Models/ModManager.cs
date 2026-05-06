using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FiveMConfigEditorWPF.Models
{
    public static class ModManager
    {
        // ── Paths ──────────────────────────────────────────────────────────────
        public static string ModsDir => Path.Combine(AppState.FiveMPath, "mods");
        public static string TempModsDir => Path.Combine(AppState.FiveMPath, "temp_mod");
        public static string PluginsDir => Path.Combine(AppState.FiveMPath, "plugins");
        public static string TempPluginsDir => Path.Combine(AppState.FiveMPath, "temp_plugin");

        // Known special plugin files/folders
        private const string UpscalerAsi      = "GTAVUpscaler.asi";
        private const string UpscalerModsDir  = "mods";          // plugins\mods
        private const string ReShadeHookDll   = "dxgi.dll";
        private const string ReShadeShadersDir = "reshade-shaders";
        private const string QuantVAddonFile  = "QuantV.addon";

        // ── Scan Mods ──────────────────────────────────────────────────────────
        public static List<ModItem> ScanMods()
        {
            var result = new List<ModItem>();
            EnsureDir(ModsDir);
            EnsureDir(TempModsDir);

            // Enabled: files/folders in mods dir
            foreach (var f in Directory.GetFiles(ModsDir, "*.rpf"))
                result.Add(MakeModItem(f, true, ModType.Rpf, false));

            foreach (var d in Directory.GetDirectories(ModsDir))
                result.Add(MakeModItem(d, true, ModType.Rpf, true));

            // Disabled: files/folders in temp_mod
            foreach (var f in Directory.GetFiles(TempModsDir, "*.rpf"))
                result.Add(MakeModItem(f, false, ModType.Rpf, false));

            foreach (var d in Directory.GetDirectories(TempModsDir))
                result.Add(MakeModItem(d, false, ModType.Rpf, true));

            return result.OrderBy(m => m.Name).ToList();
        }

        // ── Scan Plugins ───────────────────────────────────────────────────────
        public static List<ModItem> ScanPlugins()
        {
            var result = new List<ModItem>();
            EnsureDir(PluginsDir);
            EnsureDir(TempPluginsDir);

            AddPluginItems(result, PluginsDir, true);
            AddPluginItems(result, TempPluginsDir, false);

            return result.OrderBy(p => p.Name).ToList();
        }

        private static void AddPluginItems(List<ModItem> result, string baseDir, bool enabled)
        {
            if (!Directory.Exists(baseDir)) return;

            foreach (var f in Directory.GetFiles(baseDir))
            {
                var name = Path.GetFileName(f);
                var type = ClassifyPlugin(name, false);
                if (type == null) continue;
                result.Add(MakeModItem(f, enabled, type.Value, false));
            }

            foreach (var d in Directory.GetDirectories(baseDir))
            {
                var name = Path.GetFileName(d);
                var type = ClassifyPlugin(name, true);
                if (type == null) continue;
                result.Add(MakeModItem(d, enabled, type.Value, true));
            }
        }

        private static ModType? ClassifyPlugin(string name, bool isDir)
        {
            if (!isDir)
            {
                if (name.Equals(UpscalerAsi, StringComparison.OrdinalIgnoreCase))   return ModType.Upscaler;
                if (name.Equals(ReShadeHookDll, StringComparison.OrdinalIgnoreCase)) return ModType.ReShadeHook;
                if (name.Equals(QuantVAddonFile, StringComparison.OrdinalIgnoreCase)) return ModType.QuantVAddon;
                if (name.EndsWith(".asi", StringComparison.OrdinalIgnoreCase))       return ModType.Plugin;
                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))       return ModType.Plugin;
                return null; // skip unknown files
            }
            else
            {
                if (name.Equals(UpscalerModsDir, StringComparison.OrdinalIgnoreCase))   return ModType.Upscaler;
                if (name.Equals(ReShadeShadersDir, StringComparison.OrdinalIgnoreCase)) return ModType.ReShadeShaders;
                return null;
            }
        }

        // ── Enable / Disable ───────────────────────────────────────────────────
        public static void SetModEnabled(ModItem item, bool enable)
        {
            string srcBase  = enable ? TempModsDir : ModsDir;
            string dstBase  = enable ? ModsDir     : TempModsDir;
            MoveItem(item, srcBase, dstBase, enable);
        }

        public static void SetPluginEnabled(ModItem item, bool enable)
        {
            // Upscaler: move both GTAVUpscaler.asi AND plugins\mods folder together
            if (item.Type == ModType.Upscaler)
            {
                MoveUpscalerGroup(enable);
                return;
            }
            string srcBase = enable ? TempPluginsDir : PluginsDir;
            string dstBase = enable ? PluginsDir     : TempPluginsDir;
            MoveItem(item, srcBase, dstBase, enable);
        }

        private static void MoveUpscalerGroup(bool enable)
        {
            string srcBase = enable ? TempPluginsDir : PluginsDir;
            string dstBase = enable ? PluginsDir     : TempPluginsDir;

            // Move .asi
            var asiSrc = Path.Combine(srcBase, UpscalerAsi);
            var asiDst = Path.Combine(dstBase, UpscalerAsi);
            if (File.Exists(asiSrc)) File.Move(asiSrc, asiDst, true);

            // Move mods folder
            var modsSrc = Path.Combine(srcBase, UpscalerModsDir);
            var modsDst = Path.Combine(dstBase, UpscalerModsDir);
            if (Directory.Exists(modsSrc)) MoveDirectory(modsSrc, modsDst);
        }

        private static void MoveItem(ModItem item, string srcBase, string dstBase, bool enable)
        {
            EnsureDir(dstBase);
            var dst = Path.Combine(dstBase, item.Name);

            if (item.IsDirectory)
            {
                if (Directory.Exists(item.FullPath))
                    MoveDirectory(item.FullPath, dst);
            }
            else
            {
                if (File.Exists(item.FullPath))
                    File.Move(item.FullPath, dst, true);
            }

            item.FullPath  = dst;
            item.IsEnabled = enable;
        }

        // ── Graphics Preset Apply ──────────────────────────────────────────────
        public static void ApplyGraphicsPreset(GraphicsPreset preset)
        {
            var mods    = ScanMods();
            var plugins = ScanPlugins();

            foreach (var m in mods)
            {
                if (preset.ModsState.TryGetValue(m.Name, out bool shouldEnable))
                    if (m.IsEnabled != shouldEnable)
                        SetModEnabled(m, shouldEnable);
            }

            foreach (var p in plugins)
            {
                if (preset.PluginsState.TryGetValue(p.Name, out bool shouldEnable))
                    if (p.IsEnabled != shouldEnable)
                        SetPluginEnabled(p, shouldEnable);
            }
        }

        public static GraphicsPreset CaptureCurrentState(string name, string description)
        {
            var preset = new GraphicsPreset { Name = name, Description = description };
            foreach (var m in ScanMods())
                preset.ModsState[m.Name] = m.IsEnabled;
            foreach (var p in ScanPlugins())
                preset.PluginsState[p.Name] = p.IsEnabled;
            return preset;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static ModItem MakeModItem(string path, bool enabled, ModType type, bool isDir)
        {
            long size = isDir
                ? GetDirSize(path)
                : (File.Exists(path) ? new FileInfo(path).Length : 0);

            return new ModItem
            {
                Name      = Path.GetFileName(path),
                FullPath  = path,
                IsEnabled = enabled,
                Type      = type,
                IsDirectory = isDir,
                SizeBytes = size
            };
        }

        private static long GetDirSize(string path)
        {
            if (!Directory.Exists(path)) return 0;
            long total = 0;
            foreach (var f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                try { total += new FileInfo(f).Length; } catch { }
            return total;
        }

        private static void MoveDirectory(string src, string dst)
        {
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            Directory.Move(src, dst);
        }

        public static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
