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
                CmbTextureQuality.SelectedIndex = Clamp(_data.TextureQuality, 0, 4);
                CmbShaderQuality.SelectedIndex = Clamp(_data.ShaderQuality, 0, 4);
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
                CmbShadowQuality.SelectedIndex = Clamp(_data.ShadowQuality, 0, 4);
                CmbSoftShadows.SelectedIndex = Clamp(_data.Shadow_SoftShadows, 0, 5);
                ChkLongShadows.IsChecked = _data.Shadow_LongShadows;
                ChkParticleShadows.IsChecked = _data.Shadow_ParticleShadows;

                // Reflections & Effects
                CmbReflectionQuality.SelectedIndex = Clamp(_data.ReflectionQuality, 0, 4);
                CmbWaterQuality.SelectedIndex = Clamp(_data.WaterQuality, 0, 2);
                CmbParticleQuality.SelectedIndex = Clamp(_data.ParticleQuality, 0, 3);
                CmbGrassQuality.SelectedIndex = Clamp(_data.GrassQuality, 0, 4);

                // Anti-Aliasing
                CmbMSAA.SelectedIndex = Clamp(_data.MSAA, 0, 3);
                ChkFXAA.IsChecked = _data.FXAA_Enabled;
                ChkTXAA.IsChecked = _data.TXAA_Enabled;

                // Post Processing
                CmbPostFX.SelectedIndex = Clamp(_data.PostFX, 0, 4);
                ChkDoF.IsChecked = _data.DoF;
                ChkSSAO.IsChecked = _data.SSAO > 0;

                // Advanced
                CmbTessellation.SelectedIndex = Clamp(_data.Tessellation, 0, 4);
                SliderLodScale.Value = ClampDouble(_data.LodScale, 0, 2);
                SliderCityDensity.Value = ClampDouble(_data.CityDensity, 0, 1);
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
            _data.TextureQuality = CmbTextureQuality.SelectedIndex;
            _data.ShaderQuality = CmbShaderQuality.SelectedIndex;
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
            _data.ShadowQuality = CmbShadowQuality.SelectedIndex;
            _data.Shadow_SoftShadows = CmbSoftShadows.SelectedIndex;
            _data.Shadow_LongShadows = ChkLongShadows.IsChecked == true;
            _data.Shadow_ParticleShadows = ChkParticleShadows.IsChecked == true;

            // Reflections & Effects
            _data.ReflectionQuality = CmbReflectionQuality.SelectedIndex;
            _data.WaterQuality = CmbWaterQuality.SelectedIndex;
            _data.ParticleQuality = CmbParticleQuality.SelectedIndex;
            _data.GrassQuality = CmbGrassQuality.SelectedIndex;

            // Anti-Aliasing
            _data.MSAA = CmbMSAA.SelectedIndex;
            _data.FXAA_Enabled = ChkFXAA.IsChecked == true;
            _data.TXAA_Enabled = ChkTXAA.IsChecked == true;

            // Post Processing
            _data.PostFX = CmbPostFX.SelectedIndex;
            _data.DoF = ChkDoF.IsChecked == true;
            _data.SSAO = ChkSSAO.IsChecked == true ? 2 : 0;

            // Advanced
            _data.Tessellation = CmbTessellation.SelectedIndex;
            _data.LodScale = (float)SliderLodScale.Value;
            _data.CityDensity = (float)SliderCityDensity.Value;
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
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
            switch (preset)
            {
                case "Ultra":
                    _data.TextureQuality = 4;
                    _data.ShaderQuality = 4;
                    _data.ShadowQuality = 4;
                    _data.ReflectionQuality = 4;
                    _data.WaterQuality = 2;
                    _data.ParticleQuality = 3;
                    _data.GrassQuality = 4;
                    _data.PostFX = 4;
                    _data.MSAA = 3;
                    _data.AnisotropicFiltering = 16;
                    _data.Tessellation = 4;
                    _data.Shadow_SoftShadows = 5;
                    _data.Shadow_LongShadows = true;
                    _data.Shadow_ParticleShadows = true;
                    _data.SSAO = 2;
                    _data.DoF = true;
                    _data.LodScale = 1.5f;
                    _data.CityDensity = 1.0f;
                    break;

                case "High":
                    _data.TextureQuality = 3;
                    _data.ShaderQuality = 3;
                    _data.ShadowQuality = 3;
                    _data.ReflectionQuality = 3;
                    _data.WaterQuality = 1;
                    _data.ParticleQuality = 2;
                    _data.GrassQuality = 3;
                    _data.PostFX = 3;
                    _data.MSAA = 2;
                    _data.AnisotropicFiltering = 16;
                    _data.Tessellation = 3;
                    _data.Shadow_SoftShadows = 4;
                    _data.Shadow_LongShadows = true;
                    _data.Shadow_ParticleShadows = true;
                    _data.SSAO = 2;
                    _data.DoF = true;
                    _data.LodScale = 1.0f;
                    _data.CityDensity = 1.0f;
                    break;

                case "Medium":
                    _data.TextureQuality = 2;
                    _data.ShaderQuality = 2;
                    _data.ShadowQuality = 2;
                    _data.ReflectionQuality = 2;
                    _data.WaterQuality = 1;
                    _data.ParticleQuality = 1;
                    _data.GrassQuality = 2;
                    _data.PostFX = 2;
                    _data.MSAA = 0;
                    _data.AnisotropicFiltering = 8;
                    _data.Tessellation = 2;
                    _data.Shadow_SoftShadows = 3;
                    _data.Shadow_LongShadows = false;
                    _data.Shadow_ParticleShadows = true;
                    _data.SSAO = 1;
                    _data.DoF = true;
                    _data.LodScale = 1.0f;
                    _data.CityDensity = 0.8f;
                    break;

                case "Low":
                    _data.TextureQuality = 1;
                    _data.ShaderQuality = 1;
                    _data.ShadowQuality = 1;
                    _data.ReflectionQuality = 1;
                    _data.WaterQuality = 0;
                    _data.ParticleQuality = 0;
                    _data.GrassQuality = 1;
                    _data.PostFX = 1;
                    _data.MSAA = 0;
                    _data.AnisotropicFiltering = 4;
                    _data.Tessellation = 1;
                    _data.Shadow_SoftShadows = 2;
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
