using System;
using System.IO;

namespace FiveMConfigEditorWPF.Models
{
    public enum ModType { Rpf, Plugin, Upscaler, ReShadeHook, ReShadeShaders, QuantVAddon }

    public class ModItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public ModType Type { get; set; } = ModType.Rpf;
        public bool IsDirectory { get; set; } = false;

        /// <summary>Ukuran file/folder dalam bytes</summary>
        public long SizeBytes { get; set; } = 0;

        public string SizeDisplay => SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024.0:F1} KB"
            : $"{SizeBytes / (1024.0 * 1024):F1} MB";

        public string StatusLabel => IsEnabled ? "Aktif" : "Nonaktif";

        public string TypeLabel => Type switch
        {
            ModType.Rpf => "RPF Mod",
            ModType.Plugin => "Plugin (.asi)",
            ModType.Upscaler => "Upscaler",
            ModType.ReShadeHook => "ReShade Hook",
            ModType.ReShadeShaders => "ReShade Shaders",
            ModType.QuantVAddon => "QuantV Addon",
            _ => "File"
        };
    }
}
