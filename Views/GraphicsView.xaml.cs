using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Views
{
    public partial class GraphicsView : UserControl
    {
        private readonly MainWindow _main;
        private string _currentPath = "";
        private GtaGraphicsData _data = new();

        public GraphicsView(MainWindow main)
        {
            _main = main;
            
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"XAML Error:\n{ex.Message}\n\nInner:\n{ex.InnerException?.Message}", 
                    "XAML Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Set default path
                _currentPath = GtaGraphicsHelper.GetDefaultPath();
                
                // Setup slider value changed events
                if (SliderLodScale != null)
                    SliderLodScale.ValueChanged += (s, e) => TxtLodScale.Text = e.NewValue.ToString("F1");
                if (SliderCityDensity != null)
                    SliderCityDensity.ValueChanged += (s, e) => TxtCityDensity.Text = e.NewValue.ToString("F1");
                
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization Error:\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Init Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Refresh()
        {
            TxtFilePath.Text = _currentPath;
            
            // Load data
            if (File.Exists(_currentPath))
            {
                try
                {
                    _data = GtaGraphicsHelper.Load(_currentPath);
                    LoadDataToUI();
                    TxtFileStatus.Text = "✓ File found and loaded successfully";
                    TxtFileStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF00FF00"));
                }
                catch (Exception ex)
                {
                    TxtFileStatus.Text = "⚠ File found but failed to load";
                    TxtFileStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFF0000"));
                    MessageBox.Show($"Failed to load graphics settings:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Use default values
                _data = new GtaGraphicsData();
                LoadDataToUI();
                TxtFileStatus.Text = "✗ File not found (using default values)";
                TxtFileStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFA500"));
            }
        }

        private void LoadDataToUI()
        {
            try
            {
                // Texture & Quality
                CmbTextureQuality.SelectedIndex = Math.Clamp(_data.TextureQuality, 0, 2);
                CmbShaderQuality.SelectedIndex = Math.Clamp(_data.ShaderQuality, 0, 2);
                CmbAnisotropic.SelectedIndex = _data.AnisotropicFiltering switch
                {
                    0 => 0,
                    2 => 1,
                    4 => 2,
                    8 => 3,
                    16 => 4,
                    _ => 4
                };

                // Shadows
                CmbShadowQuality.SelectedIndex = Math.Clamp(_data.ShadowQuality, 0, 3);
                CmbSoftShadows.SelectedIndex = Math.Clamp(_data.Shadow_SoftShadows, 0, 5);
                ChkLongShadows.IsChecked = _data.Shadow_LongShadows;
                ChkParticleShadows.IsChecked = _data.Shadow_ParticleShadows;

                // Reflections & Effects
                CmbReflectionQuality.SelectedIndex = Math.Clamp(_data.ReflectionQuality, 0, 3);
                CmbWaterQuality.SelectedIndex = Math.Clamp(_data.WaterQuality, 0, 2);
                CmbParticleQuality.SelectedIndex = Math.Clamp(_data.ParticleQuality, 0, 2);
                CmbGrassQuality.SelectedIndex = Math.Clamp(_data.GrassQuality, 0, 3);

                // Anti-Aliasing
                CmbMSAA.SelectedIndex = _data.MSAA switch
                {
                    2 => 1,
                    4 => 2,
                    8 => 3,
                    _ => 0
                };
                ChkFXAA.IsChecked = _data.FXAA_Enabled;
                ChkTXAA.IsChecked = _data.TXAA_Enabled;

                // Post Processing
                CmbPostFX.SelectedIndex = Math.Clamp(_data.PostFX, 0, 3);
                ChkDoF.IsChecked = _data.DoF;
                ChkSSAO.IsChecked = _data.SSAO > 0;

                // Advanced
                CmbTessellation.SelectedIndex = Math.Clamp(_data.Tessellation, 0, 3);
                SliderLodScale.Value = Math.Clamp(_data.LodScale, 0.0f, 1.0f);
                SliderCityDensity.Value = Math.Clamp(_data.CityDensity, 0.0f, 1.0f);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data to UI:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private double ClampDouble(float value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void SaveUIToData()
        {
            // Texture & Quality
            _data.TextureQuality = Math.Clamp(CmbTextureQuality.SelectedIndex, 0, 2);
            _data.ShaderQuality = Math.Clamp(CmbShaderQuality.SelectedIndex, 0, 2);
            _data.AnisotropicFiltering = CmbAnisotropic.SelectedIndex switch
            {
                0 => 0,
                1 => 2,
                2 => 4,
                3 => 8,
                4 => 16,
                _ => 16
            };

            // Shadows
            _data.ShadowQuality = Math.Clamp(CmbShadowQuality.SelectedIndex, 0, 3);
            _data.Shadow_SoftShadows = Math.Clamp(CmbSoftShadows.SelectedIndex, 0, 5);
            _data.Shadow_LongShadows = ChkLongShadows.IsChecked == true;
            _data.Shadow_ParticleShadows = ChkParticleShadows.IsChecked == true;

            // Reflections & Effects
            _data.ReflectionQuality = Math.Clamp(CmbReflectionQuality.SelectedIndex, 0, 3);
            _data.WaterQuality = Math.Clamp(CmbWaterQuality.SelectedIndex, 0, 2);
            _data.ParticleQuality = Math.Clamp(CmbParticleQuality.SelectedIndex, 0, 2);
            _data.GrassQuality = Math.Clamp(CmbGrassQuality.SelectedIndex, 0, 3);

            // Anti-Aliasing
            _data.MSAA = CmbMSAA.SelectedIndex switch
            {
                1 => 2,
                2 => 4,
                3 => 8,
                _ => 0
            };
            _data.FXAA_Enabled = ChkFXAA.IsChecked == true;
            _data.TXAA_Enabled = ChkTXAA.IsChecked == true;

            // Post Processing
            _data.PostFX = Math.Clamp(CmbPostFX.SelectedIndex, 0, 3);
            _data.DoF = ChkDoF.IsChecked == true;
            _data.SSAO = ChkSSAO.IsChecked == true ? 2 : 0;

            // Advanced
            _data.Tessellation = Math.Clamp(CmbTessellation.SelectedIndex, 0, 3);
            _data.LodScale = (float)Math.Clamp(SliderLodScale.Value, 0.0, 1.0);
            _data.CityDensity = (float)Math.Clamp(SliderCityDensity.Value, 0.0, 1.0);
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select gta5_settings.xml",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(_currentPath)
            };

            if (dlg.ShowDialog() == true)
            {
                _currentPath = dlg.FileName;
                Refresh();
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
            MessageBox.Show("Graphics settings reloaded from file.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            string result = SettingsBackup.CreateBackup();
            MessageBox.Show(result, "Backup Settings",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnResetToSafe_Click(object sender, RoutedEventArgs e)
        {
            if (GtaGraphicsHelper.IsGameRunning())
            {
                MessageBox.Show(
                    "FiveM atau GTA V sedang berjalan!\n\n" +
                    "Tutup game terlebih dahulu sebelum mereset pengaturan grafis.",
                    "Game Sedang Berjalan",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Reset graphics settings ke default (Low/Safe)?\n\n" +
                "Ini akan:\n" +
                "- Backup settings saat ini\n" +
                "- Reset semua settings ke nilai aman\n" +
                "- Memperbaiki crash FiveM\n\n" +
                "Lanjutkan?",
                "Reset to Safe Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                string result = SettingsBackup.RestoreDefaultSettings();
                MessageBox.Show(result, "Reset Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reload the new settings
                Refresh();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (GtaGraphicsHelper.IsGameRunning())
            {
                MessageBox.Show(
                    "FiveM atau GTA V sedang berjalan!\n\n" +
                    "Tutup game terlebih dahulu sebelum menyimpan pengaturan grafis agar file tidak tertimpa oleh game saat keluar.",
                    "Game Sedang Berjalan",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveUIToData();
                GtaGraphicsHelper.Save(_currentPath, _data);
                MessageBox.Show($"Graphics settings saved to:\n{_currentPath}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save graphics settings:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveUIToData();
                
                var dialog = new Dialogs.GraphicsPresetDialog(_data);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open preset dialog:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLoadPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Dialogs.GraphicsPresetDialog(_data);
                dialog.Owner = Window.GetWindow(this);
                
                if (dialog.ShowDialog() == true && dialog.SelectedPreset != null)
                {
                    _data = dialog.SelectedPreset.Data;
                    LoadDataToUI();
                    MessageBox.Show($"Preset '{dialog.SelectedPreset.Name}' loaded!\n\nKlik 'Save' untuk apply ke gta5_settings.xml", 
                        "Preset Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load preset:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPresetUltra_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("Ultra");
        }

        private void BtnPresetHigh_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("High");
        }

        private void BtnPresetMedium_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("Medium");
        }

        private void BtnPresetLow_Click(object sender, RoutedEventArgs e)
        {
            ApplyPreset("Low");
        }

        private void ApplyPreset(string preset)
        {
            bool isNvidia = GtaGraphicsHelper.IsNvidiaGpu();
            switch (preset)
            {
                case "Ultra":
                    _data.TextureQuality = 2;       // Very High (Max RAGE enum)
                    _data.ShaderQuality = 2;        // Very High (Max RAGE enum)
                    _data.ShadowQuality = 3;        // Very High (Max RAGE enum)
                    _data.ReflectionQuality = 3;    // Ultra
                    _data.WaterQuality = 2;         // Very High
                    _data.ParticleQuality = 2;      // Very High (Max RAGE enum)
                    _data.GrassQuality = 3;         // Ultra
                    _data.PostFX = 3;               // Ultra
                    _data.MSAA = 4;                 // 4x (valid D3D MSAA count)
                    _data.AnisotropicFiltering = 16;
                    _data.Tessellation = 3;         // Very High (Max RAGE enum)
                    _data.Shadow_SoftShadows = isNvidia ? 5 : 3; // NVIDIA PCSS if NVIDIA, else Softest (Rockstar)
                    _data.Shadow_LongShadows = true;
                    _data.Shadow_ParticleShadows = true;
                    _data.SSAO = 2;
                    _data.DoF = true;
                    _data.LodScale = 1.0f;
                    _data.CityDensity = 1.0f;
                    break;

                case "High":
                    _data.TextureQuality = 2;       // Very High
                    _data.ShaderQuality = 1;        // High
                    _data.ShadowQuality = 2;        // High
                    _data.ReflectionQuality = 2;    // Very High
                    _data.WaterQuality = 1;         // High
                    _data.ParticleQuality = 1;      // High
                    _data.GrassQuality = 2;         // Very High
                    _data.PostFX = 2;               // Very High
                    _data.MSAA = 2;                 // 2x (valid D3D MSAA count)
                    _data.AnisotropicFiltering = 16;
                    _data.Tessellation = 2;         // High
                    _data.Shadow_SoftShadows = 3;   // Softest (Safe for all GPUs)
                    _data.Shadow_LongShadows = true;
                    _data.Shadow_ParticleShadows = true;
                    _data.SSAO = 2;
                    _data.DoF = true;
                    _data.LodScale = 1.0f;
                    _data.CityDensity = 1.0f;
                    break;

                case "Medium":
                    _data.TextureQuality = 1;       // High
                    _data.ShaderQuality = 1;        // High
                    _data.ShadowQuality = 1;        // Normal
                    _data.ReflectionQuality = 1;    // High
                    _data.WaterQuality = 1;         // High
                    _data.ParticleQuality = 1;      // High
                    _data.GrassQuality = 1;         // High
                    _data.PostFX = 1;               // High
                    _data.MSAA = 0;                 // Off
                    _data.AnisotropicFiltering = 8;
                    _data.Tessellation = 1;         // Normal
                    _data.Shadow_SoftShadows = 2;   // Softer
                    _data.Shadow_LongShadows = false;
                    _data.Shadow_ParticleShadows = true;
                    _data.SSAO = 1;
                    _data.DoF = false;
                    _data.LodScale = 0.8f;
                    _data.CityDensity = 0.8f;
                    break;

                case "Low":
                    _data.TextureQuality = 0;       // Normal (Lowest RAGE enum)
                    _data.ShaderQuality = 0;        // Normal
                    _data.ShadowQuality = 1;        // Normal
                    _data.ReflectionQuality = 0;    // Normal
                    _data.WaterQuality = 0;         // Normal
                    _data.ParticleQuality = 0;      // Normal
                    _data.GrassQuality = 0;         // Normal
                    _data.PostFX = 0;               // Normal
                    _data.MSAA = 0;                 // Off
                    _data.AnisotropicFiltering = 0; // Off
                    _data.Tessellation = 0;         // Off
                    _data.Shadow_SoftShadows = 0;   // Sharp
                    _data.Shadow_LongShadows = false;
                    _data.Shadow_ParticleShadows = false;
                    _data.SSAO = 0;
                    _data.DoF = false;
                    _data.LodScale = 0.5f;
                    _data.CityDensity = 0.5f;
                    break;
            }

            LoadDataToUI();
            MessageBox.Show($"{preset} preset applied. Click Save to write to file.", "Preset Applied",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
