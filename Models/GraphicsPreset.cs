using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class GraphicsPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Mods folder state: filename -> enabled
        public Dictionary<string, bool> ModsState { get; set; } = new();
        // Plugins state: filename -> enabled
        public Dictionary<string, bool> PluginsState { get; set; } = new();
    }

    public static class GraphicsPresetManager
    {
        private static readonly string StorePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "graphics_presets.json");

        public static List<GraphicsPreset> Load()
        {
            if (!File.Exists(StorePath)) return new();
            try { return JsonSerializer.Deserialize<List<GraphicsPreset>>(File.ReadAllText(StorePath)) ?? new(); }
            catch { return new(); }
        }

        public static void Save(List<GraphicsPreset> presets)
        {
            File.WriteAllText(StorePath,
                JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
