using System;
using System.IO;
using System.Text;

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

                if (!Directory.Exists(BackupFolder))
                    Directory.CreateDirectory(BackupFolder);

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
            if (GtaGraphicsHelper.IsGameRunning())
            {
                return "Gagal: FiveM atau GTA V sedang berjalan! Harap tutup game terlebih dahulu sebelum reset.";
            }

            try
            {
                CreateBackup();

                string gpuName = GtaGraphicsHelper.GetAuthoritativeGpuName();

                // Standard Rockstar Games RAGE v27 XML schema
                string defaultSettings = $@"<?xml version=""1.0"" encoding=""UTF-8""?>

<Settings>
  <version value=""27"" />
  <configSource>SMC_USER</configSource>
  <graphics>
    <Tessellation value=""0"" />
    <LodScale value=""0.800000"" />
    <PedLodBias value=""0.200000"" />
    <VehicleLodBias value=""0.000000"" />
    <ShadowQuality value=""1"" />
    <ReflectionQuality value=""0"" />
    <ReflectionMSAA value=""0"" />
    <SSAO value=""0"" />
    <AnisotropicFiltering value=""0"" />
    <MSAA value=""0"" />
    <MSAAFragments value=""0"" />
    <MSAAQuality value=""0"" />
    <SamplingMode value=""0"" />
    <TextureQuality value=""0"" />
    <ParticleQuality value=""0"" />
    <WaterQuality value=""0"" />
    <GrassQuality value=""0"" />
    <ShaderQuality value=""0"" />
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
    <Lighting_FogVolumes value=""false"" />
    <Shader_SSA value=""false"" />
    <DX_Version value=""2"" />
    <CityDensity value=""0.500000"" />
    <PedVarietyMultiplier value=""0.500000"" />
    <VehicleVarietyMultiplier value=""0.500000"" />
    <PostFX value=""0"" />
    <DoF value=""false"" />
    <HdStreamingInFlight value=""false"" />
    <MaxLodScale value=""0.000000"" />
    <MotionBlurStrength value=""0.000000"" />
  </graphics>
  <system>
    <numBytesPerReplayBlock value=""9000000"" />
    <numReplayBlocks value=""30"" />
    <maxSizeOfStreamingReplay value=""1024"" />
    <maxFileStoreSize value=""65536"" />
  </system>
  <audio>
    <Audio3d value=""false"" />
  </audio>
  <video>
    <AdapterIndex value=""0"" />
    <OutputIndex value=""0"" />
    <ScreenWidth value=""1920"" />
    <ScreenHeight value=""1080"" />
    <RefreshRate value=""60"" />
    <Windowed value=""2"" />
    <VSync value=""1"" />
    <Stereo value=""0"" />
    <Convergence value=""0.100000"" />
    <Separation value=""1.000000"" />
    <PauseOnFocusLoss value=""1"" />
    <AspectRatio value=""0"" />
  </video>
  <VideoCardDescription>{gpuName}</VideoCardDescription>
</Settings>";

                if (!Directory.Exists(CitizenFXFolder))
                    Directory.CreateDirectory(CitizenFXFolder);

                var tempPath = Gta5SettingsPath + ".tmp";
                File.WriteAllText(tempPath, defaultSettings, new UTF8Encoding(false));
                File.Move(tempPath, Gta5SettingsPath, true);

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