using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FiveMConfigEditorWPF.Models
{
    public class IniData
    {
        public string IVPath { get; set; } = "";
        public string SavedBuildNumber { get; set; } = "";
        public string DefaultBuild { get; set; } = "";
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
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("[") || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                int eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;

                var key = trimmed[..eq].Trim();
                var val = trimmed[(eq + 1)..].Trim();

                if (key.Equals("IVPath", StringComparison.OrdinalIgnoreCase))
                    data.IVPath = val;
                else if (key.Equals("SavedBuildNumber", StringComparison.OrdinalIgnoreCase))
                    data.SavedBuildNumber = val;
                else if (key.Equals("DefaultBuild", StringComparison.OrdinalIgnoreCase))
                    data.DefaultBuild = val;
                else if (key.Equals("ReplaceExecutable", StringComparison.OrdinalIgnoreCase))
                    data.ReplaceExecutable = val;
                else if (key.Equals("UpdateChannel", StringComparison.OrdinalIgnoreCase))
                    data.UpdateChannel = string.IsNullOrEmpty(val) ? "production" : val;
                else if (key.Equals("PoolSizesIncrease", StringComparison.OrdinalIgnoreCase))
                {
                    if (val != "{}" && val.Length > 2)
                        try { data.PoolSizes = JsonSerializer.Deserialize<Dictionary<string, int>>(val) ?? new(); } catch { }
                }
                else if (key.Equals("ReShade5", StringComparison.OrdinalIgnoreCase))
                    data.ReShade5 = val;
            }
            return data;
        }

        public static bool Save(string path, IniData data)
        {
            string poolJson = data.PoolSizes.Count == 0
                ? "{}"
                : JsonSerializer.Serialize(data.PoolSizes);
            string updateChannel = string.IsNullOrWhiteSpace(data.UpdateChannel)
                ? "production"
                : data.UpdateChannel.Trim();

            var lines = new List<string>();
            string eol = Environment.NewLine;
            string? orig = null;

            if (File.Exists(path))
            {
                orig = File.ReadAllText(path);
                if (!orig.Contains("\r\n") && orig.Contains('\n'))
                    eol = "\n";

                if (!string.IsNullOrEmpty(orig))
                {
                    lines.AddRange(orig.Replace("\r\n", "\n").Split('\n'));
                    if (lines.Count > 0 && lines[^1] == "")
                        lines.RemoveAt(lines.Count - 1);
                }
            }

            // Update keys in-place so unrelated FiveM settings (e.g. PureLevel, DefaultBuild) are preserved
            UpsertIniKey(lines, "Game", "IVPath", data.IVPath);
            UpsertIniKey(lines, "Game", "SavedBuildNumber", data.SavedBuildNumber);
            UpsertIniKey(lines, "Game", "UpdateChannel", updateChannel);
            UpsertIniKey(lines, "Game", "PoolSizesIncrease", poolJson);
            UpsertIniKey(lines, "Game", "ReplaceExecutable", data.ReplaceExecutable);
            if (!string.IsNullOrWhiteSpace(data.DefaultBuild))
                UpsertIniKey(lines, "Game", "DefaultBuild", data.DefaultBuild.Trim());

            UpsertIniKey(lines, "Addons", "ReShade5", data.ReShade5, "    ");

            string next = string.Join(eol, lines) + eol;
            if (orig != null && string.Equals(orig, next, StringComparison.Ordinal))
                return false;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string bak = path + ".bak";
            if (File.Exists(path) && !File.Exists(bak))
                File.Copy(path, bak);

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, next);
            File.Move(tmp, path, overwrite: true);
            return true;
        }

        private static void UpsertIniKey(List<string> lines, string sectionName, string key, string value, string defaultIndent = "")
        {
            int start = -1;
            int end = lines.Count;
            string targetHeader = $"[{sectionName}]";

            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i].Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    if (start >= 0)
                    {
                        end = i;
                        break;
                    }
                    if (t.Equals(targetHeader, StringComparison.OrdinalIgnoreCase))
                        start = i + 1;
                }
            }

            if (start < 0)
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                    lines.Add("");
                lines.Add(targetHeader);
                start = lines.Count;
                end = lines.Count;
            }

            for (int i = start; i < end; i++)
            {
                var raw = lines[i];
                int eq = raw.IndexOf('=');
                if (eq > 0 && raw[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    var leading = raw[..(raw.Length - raw.TrimStart().Length)];
                    var indent = string.IsNullOrEmpty(leading) ? defaultIndent : leading;
                    lines[i] = $"{indent}{key}={value}";
                    return;
                }
            }

            int insertAt = end;
            while (insertAt > start && string.IsNullOrWhiteSpace(lines[insertAt - 1]))
                insertAt--;

            lines.Insert(insertAt, $"{defaultIndent}{key}={value}");
        }
    }
}
