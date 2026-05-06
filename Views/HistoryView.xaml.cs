using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FiveMConfigEditorWPF.Models;
using FiveMConfigEditorWPF.Dialogs;

namespace FiveMConfigEditorWPF.Views
{
    public partial class HistoryView : UserControl
    {
        private readonly MainWindow _main;
        private bool _hasChanges = false;

        public HistoryView(MainWindow main)
        {
            _main = main;
            InitializeComponent();
        }

        public void Refresh()
        {
            // Update file path
            TxtFilePath.Text = string.IsNullOrEmpty(AppState.IniPath) 
                ? "Belum dipilih" 
                : AppState.IniPath;

            if (string.IsNullOrEmpty(AppState.IniPath))
            {
                TxtStatus.Text = "Pilih file CitizenFX.ini di halaman Home";
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                BtnSaveAsPreset.Visibility = Visibility.Collapsed;
                ConfigPanel.Children.Clear();
                return;
            }

            // Check if current config matches any preset
            _hasChanges = !ConfigMatchesAnyPreset();

            if (_hasChanges)
            {
                TxtStatus.Text = "⚠ Konfigurasi berbeda dari semua preset yang tersimpan";
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)); // Orange
                BtnSaveAsPreset.Visibility = Visibility.Visible;
            }
            else
            {
                TxtStatus.Text = "✓ Konfigurasi cocok dengan preset yang tersimpan";
                TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71)); // Green
                BtnSaveAsPreset.Visibility = Visibility.Collapsed;
            }

            // Display current config
            RenderCurrentConfig();
        }

        private bool ConfigMatchesAnyPreset()
        {
            if (AppState.Presets.Count == 0) return false;

            foreach (var preset in AppState.Presets)
            {
                if (ConfigEquals(AppState.Data, preset.Data))
                    return true;
            }
            return false;
        }

        private bool ConfigEquals(IniData a, IniData b)
        {
            if (a.IVPath != b.IVPath) return false;
            if (a.SavedBuildNumber != b.SavedBuildNumber) return false;
            if (a.ReplaceExecutable != b.ReplaceExecutable) return false;
            if (a.ReShade5 != b.ReShade5) return false;

            // Compare pool sizes
            if (a.PoolSizes.Count != b.PoolSizes.Count) return false;
            foreach (var kv in a.PoolSizes)
            {
                if (!b.PoolSizes.TryGetValue(kv.Key, out int val) || val != kv.Value)
                    return false;
            }

            return true;
        }

        private void RenderCurrentConfig()
        {
            ConfigPanel.Children.Clear();

            // IVPath
            ConfigPanel.Children.Add(CreateConfigRow("GTA V Path", AppState.Data.IVPath));

            // Build Number
            ConfigPanel.Children.Add(CreateConfigRow("Build Number", AppState.Data.SavedBuildNumber));

            // Replace Executable
            ConfigPanel.Children.Add(CreateConfigRow("Replace Executable", 
                AppState.Data.ReplaceExecutable == "1" ? "Ya" : "Tidak"));

            // ReShade5
            if (!string.IsNullOrEmpty(AppState.Data.ReShade5))
                ConfigPanel.Children.Add(CreateConfigRow("ReShade 5", AppState.Data.ReShade5));

            // Pool Sizes
            if (AppState.Data.PoolSizes.Count > 0)
            {
                ConfigPanel.Children.Add(CreateSectionHeader("Pool Sizes"));
                foreach (var kv in AppState.Data.PoolSizes.OrderBy(x => x.Key))
                    ConfigPanel.Children.Add(CreateConfigRow($"  {kv.Key}", kv.Value.ToString()));
            }
        }

        private Border CreateSectionHeader(string title)
        {
            return new Border
            {
                Padding = new Thickness(0, 12, 0, 6),
                Child = new TextBlock
                {
                    Text = title,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                }
            };
        }

        private Border CreateConfigRow(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelTxt = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize = 11
            };
            Grid.SetColumn(labelTxt, 0);
            grid.Children.Add(labelTxt);

            var valueTxt = new TextBlock
            {
                Text = string.IsNullOrEmpty(value) ? "(kosong)" : value,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(valueTxt, 1);
            grid.Children.Add(valueTxt);

            return new Border { Child = grid };
        }

        private void BtnSaveAsPreset_Click(object sender, RoutedEventArgs e)
        {
            // Create temporary preset for dialog
            var tempPreset = new Preset
            {
                Name = $"Preset {DateTime.Now:dd-MM-yyyy HH:mm}",
                Description = "",
                ImagePath = "",
                Data = CloneIniData(AppState.Data)
            };

            var dialog = new PresetEditDialog(tempPreset) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            try
            {
                tempPreset.CreatedAt = DateTime.Now;
                tempPreset.UpdatedAt = DateTime.Now;

                AppState.Presets.Insert(0, tempPreset);
                PresetManager.Save(AppState.Presets);

                MessageBox.Show($"Preset \"{tempPreset.Name}\" berhasil disimpan.", "Sukses",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan preset: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IniData CloneIniData(IniData source)
        {
            return new IniData
            {
                IVPath = source.IVPath,
                SavedBuildNumber = source.SavedBuildNumber,
                ReplaceExecutable = source.ReplaceExecutable,
                ReShade5 = source.ReShade5,
                PoolSizes = new System.Collections.Generic.Dictionary<string, int>(source.PoolSizes)
            };
        }
    }
}
