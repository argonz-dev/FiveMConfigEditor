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
        public static string AiApiBaseUrl { get; set; } = SecureConfig.GetApiBaseUrl();
        public static string AiApiKey { get; set; } = SecureConfig.GetApiKey();
        public static string AiModel { get; set; } = SecureConfig.GetDefaultModel();
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
                    if (!string.IsNullOrEmpty(s.AiApiBaseUrl) && (s.AiApiBaseUrl.StartsWith("http://") || s.AiApiBaseUrl.StartsWith("https://")))
                        AiApiBaseUrl = s.AiApiBaseUrl;
                    else
                        AiApiBaseUrl = SecureConfig.GetApiBaseUrl();

                    if (!string.IsNullOrEmpty(s.AiApiKey) && !s.AiApiKey.StartsWith("enx-"))
                        AiApiKey = s.AiApiKey;
                    else
                        AiApiKey = SecureConfig.GetApiKey();

                    if (!string.IsNullOrEmpty(s.AiModel))
                        AiModel = s.AiModel;
                    else
                        AiModel = SecureConfig.GetDefaultModel();
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
                    FiveMPath = FiveMPath,
                    AiApiBaseUrl = AiApiBaseUrl,
                    AiApiKey = AiApiKey,
                    AiModel = AiModel
                }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        private class AppSettings
        {
            public string IniPath { get; set; } = "";
            public string FiveMPath { get; set; } = "";
            public string AiApiBaseUrl { get; set; } = "";
            public string AiApiKey { get; set; } = "";
            public string AiModel { get; set; } = "";
        }
    }
}
