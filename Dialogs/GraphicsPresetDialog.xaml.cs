using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class GraphicsPresetDialog : Window
    {
        private List<GraphicsSettingsPreset> _presets;
        private GtaGraphicsData _currentData;
        public GraphicsSettingsPreset? SelectedPreset { get; private set; }

        public GraphicsPresetDialog(GtaGraphicsData currentData)
        {
            InitializeComponent();
            _currentData = currentData;
            _presets = GraphicsSettingsPresetManager.Load();
            RefreshList();
        }

        private void RefreshList()
        {
            LstPresets.ItemsSource = null;
            LstPresets.ItemsSource = _presets.OrderByDescending(p => p.CreatedAt);
        }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtPresetName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Masukkan nama preset!", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if preset with same name exists
            var existing = _presets.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                var result = MessageBox.Show(
                    $"Preset '{name}' sudah ada. Timpa preset yang lama?",
                    "Konfirmasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _presets.Remove(existing);
                }
                else
                {
                    return;
                }
            }

            var preset = new GraphicsSettingsPreset
            {
                Name = name,
                Description = $"Texture: {_currentData.TextureQuality}, Shadow: {_currentData.ShadowQuality}, PostFX: {_currentData.PostFX}",
                CreatedAt = DateTime.Now,
                Data = CloneGraphicsData(_currentData)
            };

            GraphicsSettingsPresetManager.Add(_presets, preset);
            _presets = GraphicsSettingsPresetManager.Load();
            RefreshList();
            TxtPresetName.Clear();

            MessageBox.Show($"Preset '{name}' berhasil disimpan!", "Sukses",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnLoadPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GraphicsSettingsPreset preset)
            {
                SelectedPreset = preset;
                DialogResult = true;
                Close();
            }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is GraphicsSettingsPreset preset)
            {
                var result = MessageBox.Show(
                    $"Hapus preset '{preset.Name}'?",
                    "Konfirmasi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    GraphicsSettingsPresetManager.Remove(_presets, preset);
                    _presets = GraphicsSettingsPresetManager.Load();
                    RefreshList();
                }
            }
        }

        private void LstPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: preview preset details
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private GtaGraphicsData CloneGraphicsData(GtaGraphicsData source)
        {
            return new GtaGraphicsData
            {
                Tessellation = source.Tessellation,
                LodScale = source.LodScale,
                PedLodBias = source.PedLodBias,
                VehicleLodBias = source.VehicleLodBias,
                ShadowQuality = source.ShadowQuality,
                ReflectionQuality = source.ReflectionQuality,
                ReflectionMSAA = source.ReflectionMSAA,
                SSAO = source.SSAO,
                AnisotropicFiltering = source.AnisotropicFiltering,
                MSAA = source.MSAA,
                MSAAFragments = source.MSAAFragments,
                MSAAQuality = source.MSAAQuality,
                SamplingMode = source.SamplingMode,
                TextureQuality = source.TextureQuality,
                ParticleQuality = source.ParticleQuality,
                WaterQuality = source.WaterQuality,
                GrassQuality = source.GrassQuality,
                ShaderQuality = source.ShaderQuality,
                Shadow_SoftShadows = source.Shadow_SoftShadows,
                UltraShadows_Enabled = source.UltraShadows_Enabled,
                Shadow_ParticleShadows = source.Shadow_ParticleShadows,
                Shadow_Distance = source.Shadow_Distance,
                Shadow_LongShadows = source.Shadow_LongShadows,
                Shadow_SplitZStart = source.Shadow_SplitZStart,
                Shadow_SplitZEnd = source.Shadow_SplitZEnd,
                Shadow_aircraftExpWeight = source.Shadow_aircraftExpWeight,
                Shadow_DisableScreenSizeCheck = source.Shadow_DisableScreenSizeCheck,
                Reflection_MipBlur = source.Reflection_MipBlur,
                FXAA_Enabled = source.FXAA_Enabled,
                TXAA_Enabled = source.TXAA_Enabled,
                Lighting_FogVolumes = source.Lighting_FogVolumes,
                Shader_SSA = source.Shader_SSA,
                DX_Version = source.DX_Version,
                CityDensity = source.CityDensity,
                PedVarietyMultiplier = source.PedVarietyMultiplier,
                VehicleVarietyMultiplier = source.VehicleVarietyMultiplier,
                PostFX = source.PostFX,
                DoF = source.DoF,
                HdStreamingInFlight = source.HdStreamingInFlight,
                MaxLodScale = source.MaxLodScale,
                MotionBlurStrength = source.MotionBlurStrength,
                ScreenWidth = source.ScreenWidth,
                ScreenHeight = source.ScreenHeight,
                RefreshRate = source.RefreshRate,
                Windowed = source.Windowed,
                VSync = source.VSync
            };
        }
    }
}
