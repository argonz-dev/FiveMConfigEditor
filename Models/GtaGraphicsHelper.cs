using System;
using System.IO;
using System.Xml.Linq;

namespace FiveMConfigEditorWPF.Models
{
    public class GtaGraphicsData
    {
        // Graphics settings
        public int Tessellation { get; set; } = 3;
        public float LodScale { get; set; } = 1.0f;
        public float PedLodBias { get; set; } = 0.2f;
        public float VehicleLodBias { get; set; } = 0.0f;
        public int ShadowQuality { get; set; } = 3;
        public int ReflectionQuality { get; set; } = 1;
        public int ReflectionMSAA { get; set; } = 0;
        public int SSAO { get; set; } = 2;
        public int AnisotropicFiltering { get; set; } = 16;
        public int MSAA { get; set; } = 0;
        public int MSAAFragments { get; set; } = 0;
        public int MSAAQuality { get; set; } = 0;
        public int SamplingMode { get; set; } = 4;
        public int TextureQuality { get; set; } = 2;
        public int ParticleQuality { get; set; } = 1;
        public int WaterQuality { get; set; } = 1;
        public int GrassQuality { get; set; } = 1;
        public int ShaderQuality { get; set; } = 2;
        public int Shadow_SoftShadows { get; set; } = 5;
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
        public int PostFX { get; set; } = 3;
        public bool DoF { get; set; } = true;
        public bool HdStreamingInFlight { get; set; } = false;
        public float MaxLodScale { get; set; } = 0.0f;
        public float MotionBlurStrength { get; set; } = 1.0f;

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

        public static GtaGraphicsData Load(string path)
        {
            if (!File.Exists(path))
                return new GtaGraphicsData();

            try
            {
                var doc = XDocument.Load(path);
                var data = new GtaGraphicsData();
                var graphics = doc.Root?.Element("graphics");
                var video = doc.Root?.Element("video");

                if (graphics != null)
                {
                    data.Tessellation = GetIntValue(graphics, "Tessellation", 3);
                    data.LodScale = GetFloatValue(graphics, "LodScale", 1.0f);
                    data.PedLodBias = GetFloatValue(graphics, "PedLodBias", 0.2f);
                    data.VehicleLodBias = GetFloatValue(graphics, "VehicleLodBias", 0.0f);
                    data.ShadowQuality = GetIntValue(graphics, "ShadowQuality", 3);
                    data.ReflectionQuality = GetIntValue(graphics, "ReflectionQuality", 1);
                    data.ReflectionMSAA = GetIntValue(graphics, "ReflectionMSAA", 0);
                    data.SSAO = GetIntValue(graphics, "SSAO", 2);
                    data.AnisotropicFiltering = GetIntValue(graphics, "AnisotropicFiltering", 16);
                    data.MSAA = GetIntValue(graphics, "MSAA", 0);
                    data.MSAAFragments = GetIntValue(graphics, "MSAAFragments", 0);
                    data.MSAAQuality = GetIntValue(graphics, "MSAAQuality", 0);
                    data.SamplingMode = GetIntValue(graphics, "SamplingMode", 4);
                    data.TextureQuality = GetIntValue(graphics, "TextureQuality", 2);
                    data.ParticleQuality = GetIntValue(graphics, "ParticleQuality", 1);
                    data.WaterQuality = GetIntValue(graphics, "WaterQuality", 1);
                    data.GrassQuality = GetIntValue(graphics, "GrassQuality", 1);
                    data.ShaderQuality = GetIntValue(graphics, "ShaderQuality", 2);
                    data.Shadow_SoftShadows = GetIntValue(graphics, "Shadow_SoftShadows", 5);
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
                    data.PostFX = GetIntValue(graphics, "PostFX", 3);
                    data.DoF = GetBoolValue(graphics, "DoF", true);
                    data.HdStreamingInFlight = GetBoolValue(graphics, "HdStreamingInFlight", false);
                    data.MaxLodScale = GetFloatValue(graphics, "MaxLodScale", 0.0f);
                    data.MotionBlurStrength = GetFloatValue(graphics, "MotionBlurStrength", 1.0f);
                }

                if (video != null)
                {
                    data.ScreenWidth = GetIntValue(video, "ScreenWidth", 1920);
                    data.ScreenHeight = GetIntValue(video, "ScreenHeight", 1080);
                    data.RefreshRate = GetIntValue(video, "RefreshRate", 60);
                    data.Windowed = GetIntValue(video, "Windowed", 2);
                    data.VSync = GetIntValue(video, "VSync", 1);
                }

                return data;
            }
            catch
            {
                return new GtaGraphicsData();
            }
        }

        public static void Save(string path, GtaGraphicsData data)
        {
            try
            {
                XDocument doc;
                if (File.Exists(path))
                {
                    doc = XDocument.Load(path);
                }
                else
                {
                    // Create new document with basic structure
                    doc = new XDocument(
                        new XDeclaration("1.0", "UTF-8", null),
                        new XElement("Settings",
                            new XElement("version", new XAttribute("value", "27")),
                            new XElement("configSource", "SMC_AUTO"),
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
                            new XElement("VideoCardDescription", "Custom")
                        )
                    );
                }

                var graphics = doc.Root?.Element("graphics");
                var video = doc.Root?.Element("video");

                if (graphics != null)
                {
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
                }

                if (video != null)
                {
                    SetValue(video, "ScreenWidth", data.ScreenWidth);
                    SetValue(video, "ScreenHeight", data.ScreenHeight);
                    SetValue(video, "RefreshRate", data.RefreshRate);
                    SetValue(video, "Windowed", data.Windowed);
                    SetValue(video, "VSync", data.VSync);
                }

                // Ensure directory exists
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                doc.Save(path);
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
