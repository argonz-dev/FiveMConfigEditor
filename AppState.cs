using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF
{
    public static class AppState
    {
        private static readonly string SettingsPath =
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static string IniPath { get; set; } = "";
        public static string FiveMPath { get; set; } = @"d:\FiveM\FiveM.app";
        public static IniData Data { get; set; } = new();
        public static List<Preset> Presets { get; set; } = new();
        public static List<Snapshot> Snapshots { get; set; } = new();
        public static bool WatcherActive { get; set; } = false;

        public static void LoadSettings()
        {
            if (!File.Exists(SettingsPath)) return;
            try
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null)
                {
                    if (!string.IsNullOrEmpty(s.IniPath) && File.Exists(s.IniPath))
                        IniPath = s.IniPath;
                    if (!string.IsNullOrEmpty(s.FiveMPath))
                        FiveMPath = s.FiveMPath;
                }
            }
            catch { }
        }

        public static void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(new AppSettings 
                { 
                    IniPath = IniPath,
                    FiveMPath = FiveMPath
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        private class AppSettings
        {
            public string IniPath { get; set; } = "";
            public string FiveMPath { get; set; } = "";
        }
    }
}
