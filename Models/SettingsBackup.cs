using System;
using System.IO;

namespace FiveMConfigEditorWPF.Models
{
    public static class SettingsBackup
    {
        private static readonly string CitizenFXFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CitizenFX"
        );

        private static readonly string Gta5SettingsPath = Path.Combine(CitizenFXFolder, "gta5_settings.xml");
        private static readonly string BackupFolder = Path.Combine(CitizenFXFolder, "Backups");

        public static string CreateBackup()
        {
            try
            {
                if (!File.Exists(Gta5SettingsPath))
                    return "File gta5_settings.xml tidak ditemukan.";

                // Create backup folder if not exists
                if (!Directory.Exists(BackupFolder))
                    Directory.CreateDirectory(BackupFolder);

                // Create backup with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(BackupFolder, $"gta5_settings_{timestamp}.xml");

                File.Copy(Gta5SettingsPath, backupPath, true);

                return $"Backup berhasil dibuat:\n{backupPath}";
            }
            catch (Exception ex)
            {
                return $"Error saat backup: {ex.Message}";
            }
        }

        public static string RestoreDefaultSettings()
        {
            try
            {
                // Create backup first
                CreateBackup();

                // Default safe settings
                string defaultSettings = @"<?xml version=""1.0"" encoding=""UTF-8""?>

<Settings>
  <version value=""27"" />
  <configSource>SMC_AUTO</configSource>
  <graphics>
    <Tessellation value=""0"" />
    <LodScale value=""1.000000"" />
    <PedLodBias value=""0.000000"" />
    <VehicleLodBias value=""0.000000"" />
    <ShadowQuality value=""2"" />
    <ReflectionQuality value=""1"" />
    <ReflectionMSAA value=""0"" />
    <SSAO value=""0"" />
    <AnisotropicFiltering value=""4"" />
    <MSAA value=""0"" />
    <MSAAFragments value=""0"" />
    <MSAAQuality value=""0"" />
    <SamplingMode value=""0"" />
    <TextureQuality value=""1"" />
    <ParticleQuality value=""1"" />
    <WaterQuality value=""1"" />
    <GrassQuality value=""1"" />
    <ShaderQuality value=""1"" />
    <Shadow_SoftShadows value=""0"" />
    <UltraShadows_Enabled value=""false"" />
    <Shadow_ParticleShadows value=""false"" />
    <Shadow_Distance value=""1.000000"" />
    <Shadow_LongShadows value=""false"" />
    <Shadow_SplitZStart value=""0.930000"" />
    <Shadow_SplitZEnd value=""0.890000"" />
    <Shadow_aircraftExpWeight value=""0.990000"" />
    <Shadow_DisableScreenSizeCheck value=""false"" />
    <Reflection_MipBlur value=""true"" />
    <FXAA_Enabled value=""true"" />
    <TXAA_Enabled value=""false"" />
    <Lighting_FogVolumes value=""true"" />
    <Shader_SSA value=""false"" />
    <DX_Version value=""2"" />
    <CityDensity value=""1.000000"" />
    <PedVarietyMultiplier value=""1.000000"" />
    <VehicleVarietyMultiplier value=""1.000000"" />
    <PostFX value=""1"" />
    <DoF value=""false"" />
    <HdStreamingInFlight value=""false"" />
    <MaxLodScale value=""0.000000"" />
    <MotionBlurStrength value=""0.000000"" />
  </graphics>
  <system>
    <numBytesPerReplayBlock value=""9000000"" />
    <numReplayBlocks value=""8"" />
  </system>
  <video>
    <AdapterIndex value=""0"" />
    <OutputIndex value=""0"" />
    <Stereo value=""0"" />
    <Windowed value=""0"" />
    <ResX value=""1920"" />
    <ResY value=""1080"" />
    <RefreshRate value=""60"" />
    <VSYNC value=""1"" />
    <Stereo3D value=""0"" />
    <Brightness value=""50"" />
    <Contrast value=""50"" />
    <Gamma value=""50"" />
    <PauseOnFocusLoss value=""1"" />
    <AspectRatio value=""0"" />
  </video>
  <videocardmemory>
    <VideoCardMemory kbytes=""4096000"" />
  </videocardmemory>
</Settings>";

                File.WriteAllText(Gta5SettingsPath, defaultSettings);

                return "Settings berhasil di-reset ke default (Low/Safe).\nBackup settings lama sudah dibuat.";
            }
            catch (Exception ex)
            {
                return $"Error saat reset: {ex.Message}";
            }
        }

        public static string GetBackupList()
        {
            try
            {
                if (!Directory.Exists(BackupFolder))
                    return "Belum ada backup.";

                var backups = Directory.GetFiles(BackupFolder, "gta5_settings_*.xml");
                if (backups.Length == 0)
                    return "Belum ada backup.";

                return $"Ditemukan {backups.Length} backup di:\n{BackupFolder}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
