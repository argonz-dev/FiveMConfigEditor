using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class Snapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime CapturedAt { get; set; } = DateTime.Now;
        public string Label { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public IniData Data { get; set; } = new();

        public override string ToString() =>
            $"[{CapturedAt:dd/MM/yyyy HH:mm:ss}] {Label}";
    }

    public static class SnapshotManager
    {
        private static readonly string StorePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots.json");

        public static List<Snapshot> Load()
        {
            if (!File.Exists(StorePath)) return new List<Snapshot>();
            try
            {
                var json = File.ReadAllText(StorePath);
                return JsonSerializer.Deserialize<List<Snapshot>>(json) ?? new();
            }
            catch { return new(); }
        }

        public static void Save(List<Snapshot> snapshots)
        {
            var json = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(StorePath, json);
        }

        public static void Add(List<Snapshot> snapshots, Snapshot snap, int maxKeep = 50)
        {
            snapshots.Insert(0, snap);
            if (snapshots.Count > maxKeep)
                snapshots.RemoveRange(maxKeep, snapshots.Count - maxKeep);
            Save(snapshots);
        }
    }
}
