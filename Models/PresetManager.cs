using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class Preset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public IniData Data { get; set; } = new();

        public override string ToString() => Name;
    }

    public static class PresetManager
    {
        private static readonly string StorePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets.json");

        public static List<Preset> Load()
        {
            if (!File.Exists(StorePath)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<Preset>>(File.ReadAllText(StorePath)) ?? new();
            }
            catch { return new(); }
        }

        public static void Save(List<Preset> presets)
        {
            File.WriteAllText(StorePath,
                JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
