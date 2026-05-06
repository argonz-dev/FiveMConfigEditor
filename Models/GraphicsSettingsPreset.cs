using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class GraphicsSettingsPreset
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public GtaGraphicsData Data { get; set; } = new();
    }

    public static class GraphicsSettingsPresetManager
    {
        private static readonly string PresetsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gta_graphics_presets.json");

        public static List<GraphicsSettingsPreset> Load()
        {
            if (!File.Exists(PresetsPath))
                return new List<GraphicsSettingsPreset>();

            try
            {
                var json = File.ReadAllText(PresetsPath);
                return JsonSerializer.Deserialize<List<GraphicsSettingsPreset>>(json) ?? new List<GraphicsSettingsPreset>();
            }
            catch
            {
                return new List<GraphicsSettingsPreset>();
            }
        }

        public static void Save(List<GraphicsSettingsPreset> presets)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(presets, options);
                File.WriteAllText(PresetsPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save graphics presets: {ex.Message}");
            }
        }

        public static void Add(List<GraphicsSettingsPreset> presets, GraphicsSettingsPreset preset)
        {
            presets.Add(preset);
            Save(presets);
        }

        public static void Remove(List<GraphicsSettingsPreset> presets, GraphicsSettingsPreset preset)
        {
            presets.Remove(preset);
            Save(presets);
        }
    }
}
