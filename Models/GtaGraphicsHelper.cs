using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace FiveMConfigEditorWPF.Models
{
    public class GtaGraphicsData
    {
        // System & Metadata
        public string ConfigSource { get; set; } = "SMC_USER";
        public string VideoCardDescription { get; set; } = "";

        // Graphics settings (Strict RAGE Engine v27 Schema)
        public int Tessellation { get; set; } = 3;           // 0..3 (Off, Normal, High, Very High)
        public float LodScale { get; set; } = 1.0f;          // 0.0..1.0
        public float PedLodBias { get; set; } = 0.2f;        // 0.0..1.0
        public float VehicleLodBias { get; set; } = 0.0f;    // 0.0..1.0
        public int ShadowQuality { get; set; } = 3;          // 0..3 (Off, Normal, High, Very High)
        public int ReflectionQuality { get; set; } = 2;      // 0..3 (Normal, High, Very High, Ultra)
        public int ReflectionMSAA { get; set; } = 0;         // 0, 2, 4, 8
        public int SSAO { get; set; } = 2;                   // 0..2 (Off, Normal, High)
        public int AnisotropicFiltering { get; set; } = 16;  // 0, 2, 4, 8, 16
        public int MSAA { get; set; } = 0;                   // 0, 2, 4, 8
        public int MSAAFragments { get; set; } = 0;
        public int MSAAQuality { get; set; } = 0;
        public int SamplingMode { get; set; } = 0;           // 0..8 (0 = Off)
        public int TextureQuality { get; set; } = 2;         // 0..2 (Normal, High, Very High)
        public int ParticleQuality { get; set; } = 2;        // 0..2 (Normal, High, Very High)
        public int WaterQuality { get; set; } = 2;           // 0..2 (Normal, High, Very High)
        public int GrassQuality { get; set; } = 2;           // 0..3 (Normal, High, Very High, Ultra)
        public int ShaderQuality { get; set; } = 2;          // 0..2 (Normal, High, Very High)
        public int Shadow_SoftShadows { get; set; } = 3;     // 0..5 (0=Sharp,1=Soft,2=Softer,3=Softest,4=AMD CHS,5=NVIDIA PCSS)
        public bool UltraShadows_Enabled { get; set; } = false;
        public bool Shadow_ParticleShadows { get; set; } = true;
        public float Shadow_Distance { get; set; } = 1.0f;
        public bool Shadow_LongShadows { get; set; } = false;
        public float Shadow_SplitZStart { get; set; } = 0.93f;
        public float Shadow_SplitZEnd { get; set; } = 0.89f;
        public float Shadow_aircraftExpWeight { get; set; } = 0.99f;
        public bool Shadow_DisableScreenSizeCheck { get; set; } = false;
        public bool Reflection_MipBlur { get; set; } = true;
        public bool FXAA_Enabled { get; set; } = false;
        public bool TXAA_Enabled { get; set; } = false;
        public bool Lighting_FogVolumes { get; set; } = true;
        public bool Shader_SSA { get; set; } = true;
        public int DX_Version { get; set; } = 2;
        public float CityDensity { get; set; } = 1.0f;
        public float PedVarietyMultiplier { get; set; } = 1.0f;
        public float VehicleVarietyMultiplier { get; set; } = 1.0f;
        public int PostFX { get; set; } = 2;
        public bool DoF { get; set; } = true;
        public bool HdStreamingInFlight { get; set; } = false;
        public float MaxLodScale { get; set; } = 0.0f;
        public float MotionBlurStrength { get; set; } = 0.0f;

        // Video settings
        public int ScreenWidth { get; set; } = 1920;
        public int ScreenHeight { get; set; } = 1080;
        public int RefreshRate { get; set; } = 60;
        public int Windowed { get; set; } = 2; // 0=fullscreen, 1=windowed, 2=borderless
        public int VSync { get; set; } = 1;
    }

    public static class GtaGraphicsHelper
    {
        public static string GetDefaultPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "CitizenFX", "gta5_settings.xml");
        }

        public static string GetRockstarPath()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(docs, "Rockstar Games", "GTA V", "settings.xml");
        }

        public static bool IsGameRunning()
        {
            var targetNames = new[] { "FiveM", "GTA5", "PlayGTAV" };
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    var name = p.ProcessName;
                    if (targetNames.Any(t => string.Equals(name, t, StringComparison.OrdinalIgnoreCase)) ||
                        (name.StartsWith("FiveM_", StringComparison.OrdinalIgnoreCase) && name.EndsWith("GTAProcess", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        public static string GetAuthoritativeGpuName()
        {
            // 1. Authoritative Rockstar Games settings.xml
            try
            {
                var rPath = GetRockstarPath();
                if (File.Exists(rPath))
                {
                    var doc = XDocument.Load(rPath);
                    var desc = doc.Root?.Element("VideoCardDescription")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(desc) && !string.Equals(desc, "Custom", StringComparison.OrdinalIgnoreCase))
                        return desc;
                }
            }
            catch { }

            // 2. Existing CitizenFX settings.xml
            try
            {
                var cPath = GetDefaultPath();
                if (File.Exists(cPath))
                {
                    var doc = XDocument.Load(cPath);
                    var desc = doc.Root?.Element("VideoCardDescription")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(desc) && !string.Equals(desc, "Custom", StringComparison.OrdinalIgnoreCase))
                        return desc;
                }
            }
            catch { }

            // 3. Registry DriverDesc lookup
            try
            {
                using var classKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                if (classKey != null)
                {
                    foreach (var subKeyName in classKey.GetSubKeyNames())
                    {
                        if (subKeyName.Length == 4 && int.TryParse(subKeyName, out _))
                        {
                            using var subKey = classKey.OpenSubKey(subKeyName);
                            var desc = subKey?.GetValue("DriverDesc")?.ToString();
                            if (!string.IsNullOrEmpty(desc) && !desc.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                                return desc;
                        }
                    }
                }
            }
            catch { }

            return "NVIDIA GeForce RTX 5060 Ti";
        }

        public static bool IsNvidiaGpu()
        {
            var gpu = GetAuthoritativeGpuName();
            return gpu.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                   gpu.Contains("GeForce", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAmdGpu()
        {
            var gpu = GetAuthoritativeGpuName();
            return gpu.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
                   gpu.Contains("Radeon", StringComparison.OrdinalIgnoreCase);
        }

        public static void Sanitize(GtaGraphicsData data)
        {
            data.TextureQuality = Math.Clamp(data.TextureQuality, 0, 2);
            data.ShaderQuality = Math.Clamp(data.ShaderQuality, 0, 2);
            data.ShadowQuality = Math.Clamp(data.ShadowQuality, 0, 3);
            data.ReflectionQuality = Math.Clamp(data.ReflectionQuality, 0, 3);
            data.WaterQuality = Math.Clamp(data.WaterQuality, 0, 2);
            data.ParticleQuality = Math.Clamp(data.ParticleQuality, 0, 2);
            data.GrassQuality = Math.Clamp(data.GrassQuality, 0, 3);
            data.PostFX = Math.Clamp(data.PostFX, 0, 3);
            data.Tessellation = Math.Clamp(data.Tessellation, 0, 3);
            data.SSAO = Math.Clamp(data.SSAO, 0, 2);

            // Normalize MSAA to valid RAGE / DirectX values: 0, 2, 4, 8
            data.MSAA = data.MSAA switch
            {
                >= 8 => 8,
                >= 4 => 4,
                >= 2 => 2,
                _ => 0
            };

            data.ReflectionMSAA = data.ReflectionMSAA switch
            {
                >= 8 => 8,
                >= 4 => 4,
                >= 2 => 2,
                _ => 0
            };

            data.AnisotropicFiltering = data.AnisotropicFiltering switch
            {
                >= 16 => 16,
                >= 8 => 8,
                >= 4 => 4,
                >= 2 => 2,
                _ => 0
            };

            data.SamplingMode = Math.Clamp(data.SamplingMode, 0, 8);
            data.DX_Version = Math.Clamp(data.DX_Version, 0, 2);

            // Vendor-specific soft shadow guard
            if (data.Shadow_SoftShadows == 5 && !IsNvidiaGpu())
            {
                data.Shadow_SoftShadows = 3; // Fallback to Softest
            }
            else if (data.Shadow_SoftShadows == 4 && !IsAmdGpu())
            {
                data.Shadow_SoftShadows = 3; // Fallback to Softest
            }
            else
            {
                data.Shadow_SoftShadows = Math.Clamp(data.Shadow_SoftShadows, 0, 5);
            }

            data.LodScale = Math.Clamp(data.LodScale, 0.0f, 1.0f);
            data.CityDensity = Math.Clamp(data.CityDensity, 0.0f, 1.0f);
            data.PedVarietyMultiplier = Math.Clamp(data.PedVarietyMultiplier, 0.0f, 1.0f);
            data.VehicleVarietyMultiplier = Math.Clamp(data.VehicleVarietyMultiplier, 0.0f, 1.0f);

            if (string.IsNullOrWhiteSpace(data.VideoCardDescription) ||
                string.Equals(data.VideoCardDescription, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                data.VideoCardDescription = GetAuthoritativeGpuName();
            }
        }

        public static GtaGraphicsData Load(string path)
        {
            if (!File.Exists(path))
            {
                var fallback = new GtaGraphicsData();
                fallback.VideoCardDescription = GetAuthoritativeGpuName();
                return fallback;
            }

            try
            {
                var doc = XDocument.Load(path);
                var data = new GtaGraphicsData();
                data.ConfigSource = doc.Root?.Element("configSource")?.Value ?? "SMC_USER";
                data.VideoCardDescription = doc.Root?.Element("VideoCardDescription")?.Value?.Trim() ?? "";

                var graphics = doc.Root?.Element("graphics");
                var video = doc.Root?.Element("video");

                if (graphics != null)
                {
                    data.Tessellation = GetIntValue(graphics, "Tessellation", 3);
                    data.LodScale = GetFloatValue(graphics, "LodScale", 1.0f);
                    data.PedLodBias = GetFloatValue(graphics, "PedLodBias", 0.2f);
                    data.VehicleLodBias = GetFloatValue(graphics, "VehicleLodBias", 0.0f);
                    data.ShadowQuality = GetIntValue(graphics, "ShadowQuality", 3);
                    data.ReflectionQuality = GetIntValue(graphics, "ReflectionQuality", 2);
                    data.ReflectionMSAA = GetIntValue(graphics, "ReflectionMSAA", 0);
                    data.SSAO = GetIntValue(graphics, "SSAO", 2);
                    data.AnisotropicFiltering = GetIntValue(graphics, "AnisotropicFiltering", 16);
                    data.MSAA = GetIntValue(graphics, "MSAA", 0);
                    data.MSAAFragments = GetIntValue(graphics, "MSAAFragments", 0);
                    data.MSAAQuality = GetIntValue(graphics, "MSAAQuality", 0);
                    data.SamplingMode = GetIntValue(graphics, "SamplingMode", 0);
                    data.TextureQuality = GetIntValue(graphics, "TextureQuality", 2);
                    data.ParticleQuality = GetIntValue(graphics, "ParticleQuality", 2);
                    data.WaterQuality = GetIntValue(graphics, "WaterQuality", 2);
                    data.GrassQuality = GetIntValue(graphics, "GrassQuality", 2);
                    data.ShaderQuality = GetIntValue(graphics, "ShaderQuality", 2);
                    data.Shadow_SoftShadows = GetIntValue(graphics, "Shadow_SoftShadows", 3);
                    data.UltraShadows_Enabled = GetBoolValue(graphics, "UltraShadows_Enabled", false);
                    data.Shadow_ParticleShadows = GetBoolValue(graphics, "Shadow_ParticleShadows", true);
                    data.Shadow_Distance = GetFloatValue(graphics, "Shadow_Distance", 1.0f);
                    data.Shadow_LongShadows = GetBoolValue(graphics, "Shadow_LongShadows", false);
                    data.Shadow_SplitZStart = GetFloatValue(graphics, "Shadow_SplitZStart", 0.93f);
                    data.Shadow_SplitZEnd = GetFloatValue(graphics, "Shadow_SplitZEnd", 0.89f);
                    data.Shadow_aircraftExpWeight = GetFloatValue(graphics, "Shadow_aircraftExpWeight", 0.99f);
                    data.Shadow_DisableScreenSizeCheck = GetBoolValue(graphics, "Shadow_DisableScreenSizeCheck", false);
                    data.Reflection_MipBlur = GetBoolValue(graphics, "Reflection_MipBlur", true);
                    data.FXAA_Enabled = GetBoolValue(graphics, "FXAA_Enabled", false);
                    data.TXAA_Enabled = GetBoolValue(graphics, "TXAA_Enabled", false);
                    data.Lighting_FogVolumes = GetBoolValue(graphics, "Lighting_FogVolumes", true);
                    data.Shader_SSA = GetBoolValue(graphics, "Shader_SSA", true);
                    data.DX_Version = GetIntValue(graphics, "DX_Version", 2);
                    data.CityDensity = GetFloatValue(graphics, "CityDensity", 1.0f);
                    data.PedVarietyMultiplier = GetFloatValue(graphics, "PedVarietyMultiplier", 1.0f);
                    data.VehicleVarietyMultiplier = GetFloatValue(graphics, "VehicleVarietyMultiplier", 1.0f);
                    data.PostFX = GetIntValue(graphics, "PostFX", 2);
                    data.DoF = GetBoolValue(graphics, "DoF", true);
                    data.HdStreamingInFlight = GetBoolValue(graphics, "HdStreamingInFlight", false);
                    data.MaxLodScale = GetFloatValue(graphics, "MaxLodScale", 0.0f);
                    data.MotionBlurStrength = GetFloatValue(graphics, "MotionBlurStrength", 0.0f);
                }

                if (video != null)
                {
                    data.ScreenWidth = GetIntValue(video, "ScreenWidth", 1920);
                    data.ScreenHeight = GetIntValue(video, "ScreenHeight", 1080);
                    data.RefreshRate = GetIntValue(video, "RefreshRate", 60);
                    data.Windowed = GetIntValue(video, "Windowed", 2);
                    data.VSync = GetIntValue(video, "VSync", 1);
                }

                Sanitize(data);
                return data;
            }
            catch
            {
                var fallback = new GtaGraphicsData();
                fallback.VideoCardDescription = GetAuthoritativeGpuName();
                return fallback;
            }
        }

        public static void Save(string path, GtaGraphicsData data)
        {
            if (IsGameRunning())
            {
                throw new InvalidOperationException("FiveM atau GTA V sedang berjalan! Tutup game terlebih dahulu sebelum menyimpan pengaturan.");
            }

            try
            {
                Sanitize(data);

                // Auto-backup before overwriting
                if (File.Exists(path))
                {
                    SettingsBackup.CreateBackup();
                }

                XDocument doc;
                if (File.Exists(path))
                {
                    doc = XDocument.Load(path);
                }
                else
                {
                    // Create new document with standard Rockstar RAGE v27 structure
                    doc = new XDocument(
                        new XDeclaration("1.0", "UTF-8", null),
                        new XElement("Settings",
                            new XElement("version", new XAttribute("value", "27")),
                            new XElement("configSource", "SMC_USER"),
                            new XElement("graphics"),
                            new XElement("system",
                                new XElement("numBytesPerReplayBlock", new XAttribute("value", "9000000")),
                                new XElement("numReplayBlocks", new XAttribute("value", "30")),
                                new XElement("maxSizeOfStreamingReplay", new XAttribute("value", "1024")),
                                new XElement("maxFileStoreSize", new XAttribute("value", "65536"))
                            ),
                            new XElement("audio",
                                new XElement("Audio3d", new XAttribute("value", "false"))
                            ),
                            new XElement("video"),
                            new XElement("VideoCardDescription", data.VideoCardDescription)
                        )
                    );
                }

                // Maintain SMC_USER configSource
                var cfgElem = doc.Root?.Element("configSource");
                if (cfgElem != null)
                    cfgElem.Value = "SMC_USER";

                // Ensure VideoCardDescription is populated with real GPU name
                var gpuElem = doc.Root?.Element("VideoCardDescription");
                if (gpuElem == null)
                {
                    doc.Root?.Add(new XElement("VideoCardDescription", data.VideoCardDescription));
                }
                else if (string.IsNullOrWhiteSpace(gpuElem.Value) || string.Equals(gpuElem.Value, "Custom", StringComparison.OrdinalIgnoreCase))
                {
                    gpuElem.Value = data.VideoCardDescription;
                }

                var graphics = doc.Root?.Element("graphics");
                if (graphics == null)
                {
                    graphics = new XElement("graphics");
                    doc.Root?.Add(graphics);
                }

                SetValue(graphics, "Tessellation", data.Tessellation);
                SetValue(graphics, "LodScale", data.LodScale);
                SetValue(graphics, "PedLodBias", data.PedLodBias);
                SetValue(graphics, "VehicleLodBias", data.VehicleLodBias);
                SetValue(graphics, "ShadowQuality", data.ShadowQuality);
                SetValue(graphics, "ReflectionQuality", data.ReflectionQuality);
                SetValue(graphics, "ReflectionMSAA", data.ReflectionMSAA);
                SetValue(graphics, "SSAO", data.SSAO);
                SetValue(graphics, "AnisotropicFiltering", data.AnisotropicFiltering);
                SetValue(graphics, "MSAA", data.MSAA);
                SetValue(graphics, "MSAAFragments", data.MSAAFragments);
                SetValue(graphics, "MSAAQuality", data.MSAAQuality);
                SetValue(graphics, "SamplingMode", data.SamplingMode);
                SetValue(graphics, "TextureQuality", data.TextureQuality);
                SetValue(graphics, "ParticleQuality", data.ParticleQuality);
                SetValue(graphics, "WaterQuality", data.WaterQuality);
                SetValue(graphics, "GrassQuality", data.GrassQuality);
                SetValue(graphics, "ShaderQuality", data.ShaderQuality);
                SetValue(graphics, "Shadow_SoftShadows", data.Shadow_SoftShadows);
                SetValue(graphics, "UltraShadows_Enabled", data.UltraShadows_Enabled);
                SetValue(graphics, "Shadow_ParticleShadows", data.Shadow_ParticleShadows);
                SetValue(graphics, "Shadow_Distance", data.Shadow_Distance);
                SetValue(graphics, "Shadow_LongShadows", data.Shadow_LongShadows);
                SetValue(graphics, "Shadow_SplitZStart", data.Shadow_SplitZStart);
                SetValue(graphics, "Shadow_SplitZEnd", data.Shadow_SplitZEnd);
                SetValue(graphics, "Shadow_aircraftExpWeight", data.Shadow_aircraftExpWeight);
                SetValue(graphics, "Shadow_DisableScreenSizeCheck", data.Shadow_DisableScreenSizeCheck);
                SetValue(graphics, "Reflection_MipBlur", data.Reflection_MipBlur);
                SetValue(graphics, "FXAA_Enabled", data.FXAA_Enabled);
                SetValue(graphics, "TXAA_Enabled", data.TXAA_Enabled);
                SetValue(graphics, "Lighting_FogVolumes", data.Lighting_FogVolumes);
                SetValue(graphics, "Shader_SSA", data.Shader_SSA);
                SetValue(graphics, "DX_Version", data.DX_Version);
                SetValue(graphics, "CityDensity", data.CityDensity);
                SetValue(graphics, "PedVarietyMultiplier", data.PedVarietyMultiplier);
                SetValue(graphics, "VehicleVarietyMultiplier", data.VehicleVarietyMultiplier);
                SetValue(graphics, "PostFX", data.PostFX);
                SetValue(graphics, "DoF", data.DoF);
                SetValue(graphics, "HdStreamingInFlight", data.HdStreamingInFlight);
                SetValue(graphics, "MaxLodScale", data.MaxLodScale);
                SetValue(graphics, "MotionBlurStrength", data.MotionBlurStrength);

                var video = doc.Root?.Element("video");
                if (video == null)
                {
                    video = new XElement("video");
                    doc.Root?.Add(video);
                }

                SetValue(video, "ScreenWidth", data.ScreenWidth);
                SetValue(video, "ScreenHeight", data.ScreenHeight);
                SetValue(video, "RefreshRate", data.RefreshRate);
                SetValue(video, "Windowed", data.Windowed);
                SetValue(video, "VSync", data.VSync);

                // Ensure directory exists
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Atomic write with UTF-8 without BOM and CRLF
                var tempPath = path + ".tmp";
                var xmlSettings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(tempPath, xmlSettings))
                {
                    doc.Save(writer);
                }

                File.Move(tempPath, path, true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save graphics settings: {ex.Message}");
            }
        }

        private static int GetIntValue(XElement parent, string name, int defaultValue)
        {
            var elem = parent.Element(name);
            if (elem == null) return defaultValue;
            var attr = elem.Attribute("value");
            if (attr == null) return defaultValue;
            return int.TryParse(attr.Value, out var val) ? val : defaultValue;
        }

        private static float GetFloatValue(XElement parent, string name, float defaultValue)
        {
            var elem = parent.Element(name);
            if (elem == null) return defaultValue;
            var attr = elem.Attribute("value");
            if (attr == null) return defaultValue;
            return float.TryParse(attr.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : defaultValue;
        }

        private static bool GetBoolValue(XElement parent, string name, bool defaultValue)
        {
            var elem = parent.Element(name);
            if (elem == null) return defaultValue;
            var attr = elem.Attribute("value");
            if (attr == null) return defaultValue;
            return attr.Value.ToLower() == "true";
        }

        private static void SetValue(XElement parent, string name, object value)
        {
            var elem = parent.Element(name);
            string strValue = value switch
            {
                bool b => b ? "true" : "false",
                float f => f.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? ""
            };

            if (elem == null)
            {
                parent.Add(new XElement(name, new XAttribute("value", strValue)));
            }
            else
            {
                var attr = elem.Attribute("value");
                if (attr == null)
                    elem.Add(new XAttribute("value", strValue));
                else
                    attr.Value = strValue;
            }
        }
    }
}