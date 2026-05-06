using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class IniData
    {
        public string IVPath { get; set; } = "";
        public string SavedBuildNumber { get; set; } = "";
        public string ReplaceExecutable { get; set; } = "0";
        public Dictionary<string, int> PoolSizes { get; set; } = new();
        public string ReShade5 { get; set; } = "";
        public string UpdateChannel { get; set; } = "production"; // beta or production
    }

    public static class IniHelper
    {
        public static IniData Load(string path)
        {
            if (!File.Exists(path)) return new IniData();
            return ParseText(File.ReadAllText(path));
        }

        public static IniData ParseText(string text)
        {
            var data = new IniData();
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("IVPath="))
                    data.IVPath = trimmed["IVPath=".Length..];
                else if (trimmed.StartsWith("SavedBuildNumber="))
                    data.SavedBuildNumber = trimmed["SavedBuildNumber=".Length..];
                else if (trimmed.StartsWith("ReplaceExecutable="))
                    data.ReplaceExecutable = trimmed["ReplaceExecutable=".Length..];
                else if (trimmed.StartsWith("UpdateChannel="))
                    data.UpdateChannel = trimmed["UpdateChannel=".Length..];
                else if (trimmed.StartsWith("PoolSizesIncrease="))
                {
                    var json = trimmed["PoolSizesIncrease=".Length..];
                    if (json != "{}" && json.Length > 2)
                        try { data.PoolSizes = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new(); } catch { }
                }
                else if (trimmed.StartsWith("ReShade5="))
                    data.ReShade5 = trimmed["ReShade5=".Length..];
            }
            return data;
        }

        public static void Save(string path, IniData data)
        {
            string poolJson = data.PoolSizes.Count == 0
                ? "{}"
                : JsonSerializer.Serialize(data.PoolSizes);

            var lines = new List<string>
            {
                "[Game]",
                $"IVPath={data.IVPath}",
                $"SavedBuildNumber={data.SavedBuildNumber}",
                $"UpdateChannel={data.UpdateChannel}",
                $"PoolSizesIncrease={poolJson}",
                $"ReplaceExecutable={data.ReplaceExecutable}",
                "",
                "[Addons]",
                $"    ReShade5={data.ReShade5}"
            };
            File.WriteAllLines(path, lines);
        }
    }
}
