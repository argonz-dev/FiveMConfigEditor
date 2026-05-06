using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;
using FiveMConfigEditorWPF.Dialogs;
namespace FiveMConfigEditorWPF.Views
{
    public partial class HomeView : UserControl
    {
        private readonly MainWindow _main;
        private DispatcherTimer? _statusTimer;

        public HomeView(MainWindow main)
        {
            _main = main;
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            if (!string.IsNullOrEmpty(AppState.IniPath))
            {
                TxtFilePath.Text = AppState.IniPath;
                TxtPlaceholder.Visibility = Visibility.Collapsed;
            }

            bool active = AppState.WatcherActive;
            LedWatcher.Fill = active
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            TxtWatcherStatus.Text      = active ? "Status Watcher: Aktif" : "Status Watcher: Belum Aktif";
            TxtWatcherStatus.Foreground = active
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

            RenderPresets();
        }

        public void ShowStatus(string msg, bool success = true)
        {
            TxtStatus.Text       = msg;
            TxtStatus.Foreground = success
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x4D));

            _statusTimer?.Stop();
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusTimer.Tick += (_, _) => { TxtStatus.Text = ""; _statusTimer.Stop(); };
            _statusTimer.Start();
        }

        private void RenderPresets()
        {
            PresetPanel.Children.Clear();
            if (AppState.Presets.Count == 0)
            {
                PresetPanel.Children.Add(new TextBlock
                {
                    Text = "Belum ada preset. Klik \"+ Buat Preset Baru\" untuk memulai.",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88)),
                    FontSize = 12, Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                return;
            }
            foreach (var preset in AppState.Presets)
                PresetPanel.Children.Add(CreatePresetCard(preset));
        }

        private Border CreatePresetCard(Preset preset)
        {
            var card = new Border
            {
                Margin        = new Thickness(0, 0, 0, 10),
                CornerRadius  = new CornerRadius(12),
                Background    = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                BorderBrush   = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x4A)),
                BorderThickness = new Thickness(1),
                Padding       = new Thickness(14, 12, 14, 12),
                Cursor        = Cursors.Arrow,
                Effect        = new DropShadowEffect
                {
                    Color = Color.FromRgb(0x00, 0x78, 0xD4),
                    BlurRadius = 15, ShadowDepth = 0, Opacity = 0.15
                }
            };
            card.MouseEnter += (_, _) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
                ((DropShadowEffect)card.Effect).Opacity = 0.4;
            };
            card.MouseLeave += (_, _) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x4A));
                ((DropShadowEffect)card.Effect).Opacity = 0.15;
            };

            var outerGrid = new Grid();
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Thumbnail
            if (!string.IsNullOrEmpty(preset.ImagePath) && File.Exists(preset.ImagePath))
            {
                var imgSrc = ImageHelper.LoadImage(preset.ImagePath);
                if (imgSrc != null)
                {
                    var img = new System.Windows.Controls.Image
                    {
                        Source = imgSrc,
                        Width = 48, Height = 48,
                        Margin = new Thickness(0, 0, 12, 0),
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(img, 0);
                    outerGrid.Children.Add(img);
                }
            }

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(stack, 1);

            stack.Children.Add(new TextBlock
            {
                Text = preset.Name, Foreground = Brushes.White,
                FontSize = 13, FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 3)
            });

            string desc = string.IsNullOrEmpty(preset.Description)
                ? $"Build: {preset.Data.SavedBuildNumber}  •  Pools: {preset.Data.PoolSizes.Count}"
                : preset.Description;
            stack.Children.Add(new TextBlock
            {
                Text = desc, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 2)
            });
            stack.Children.Add(new TextBlock
            {
                Text = $"Diperbarui: {preset.UpdatedAt:dd/MM/yyyy}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88)),
                FontSize = 10, Margin = new Thickness(0, 0, 0, 10)
            });

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };

            var btnApply = new Button { Content = "▶ Terapkan", Width = 100, Margin = new Thickness(0, 0, 6, 0) };
            btnApply.Style = (Style)Application.Current.FindResource("NeonButtonSmallStyle");
            btnApply.Click += (_, _) => ApplyPreset(preset);

            var btnConfig = new Button { Content = "⚙ Config", Width = 80, Margin = new Thickness(0, 0, 6, 0) };
            btnConfig.Style = (Style)Application.Current.FindResource("NeonButtonSmallStyle");
            btnConfig.Click += (_, _) => OpenPresetConfig(preset);

            var btnEdit = new Button { Content = "✎ Edit", Width = 72, Margin = new Thickness(0, 0, 6, 0) };
            btnEdit.Style = (Style)Application.Current.FindResource("GreenButtonSmallStyle");
            btnEdit.Click += (_, _) => EditPreset(preset);

            var btnDelete = new Button { Content = "✕ Hapus", Width = 80 };
            btnDelete.Style = (Style)Application.Current.FindResource("RedButtonSmallStyle");
            btnDelete.Click += (_, _) => DeletePreset(preset);

            btnRow.Children.Add(btnApply);
            btnRow.Children.Add(btnConfig);
            btnRow.Children.Add(btnEdit);
            btnRow.Children.Add(btnDelete);
            stack.Children.Add(btnRow);

            outerGrid.Children.Add(stack);
            card.Child = outerGrid;
            return card;
        }

        private void ApplyPreset(Preset preset)
        {
            if (string.IsNullOrEmpty(AppState.IniPath))
            {
                ShowStatus("⚠ Pilih file .ini terlebih dahulu!", false);
                return;
            }
            AppState.Data = CloneData(preset.Data);
            IniHelper.Save(AppState.IniPath, AppState.Data);
            SnapshotManager.Add(AppState.Snapshots, new Snapshot
            {
                Label = $"Terapkan: {preset.Name}",
                SourceFile = AppState.IniPath,
                Data = CloneData(preset.Data),
                CapturedAt = DateTime.Now
            });
            ShowStatus($"✔ Preset \"{preset.Name}\" berhasil diterapkan ke {Path.GetFileName(AppState.IniPath)}");
        }

        private void OpenPresetConfig(Preset preset)
        {
            var dlg = new PresetConfigDialog(preset) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                preset.UpdatedAt = DateTime.Now;
                PresetManager.Save(AppState.Presets);
                RenderPresets();
                ShowStatus($"✔ Config preset \"{preset.Name}\" disimpan.");
            }
        }

        private void EditPreset(Preset preset)
        {
            var dlg = new PresetEditDialog(preset) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                PresetManager.Save(AppState.Presets);
                RenderPresets();
            }
        }

        private void DeletePreset(Preset preset)
        {
            if (MessageBox.Show($"Hapus preset \"{preset.Name}\"?", "Konfirmasi",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            AppState.Presets.Remove(preset);
            PresetManager.Save(AppState.Presets);
            RenderPresets();
        }

        private void BtnNewPreset_Click(object sender, RoutedEventArgs e)
        {
            var newPreset = new Preset
            {
                Name = "Preset Baru",
                Data = AppState.Data != null ? CloneData(AppState.Data) : new IniData()
            };
            var dlg = new PresetEditDialog(newPreset) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                AppState.Presets.Add(newPreset);
                PresetManager.Save(AppState.Presets);
                RenderPresets();
                ShowStatus($"✔ Preset \"{newPreset.Name}\" dibuat.");
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*",
                Title = "Pilih file konfigurasi .ini"
            };
            if (dlg.ShowDialog() == true)
            {
                TxtFilePath.Text = dlg.FileName;
                TxtPlaceholder.Visibility = Visibility.Collapsed;
                _main.SetIniPath(dlg.FileName);
                Refresh();
            }
        }

        private void BtnAutoDetect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AppState.FiveMPath) || !Directory.Exists(AppState.FiveMPath))
            {
                ShowStatus("⚠ Folder FiveM belum dikonfigurasi. Silakan ke menu Config.", false);
                return;
            }

            var iniPath = Path.Combine(AppState.FiveMPath, "CitizenFX.ini");
            if (!File.Exists(iniPath))
            {
                ShowStatus($"⚠ CitizenFX.ini tidak ditemukan di: {AppState.FiveMPath}", false);
                MessageBox.Show($"CitizenFX.ini tidak ditemukan di folder FiveM:\n{AppState.FiveMPath}\n\nPastikan folder FiveM sudah benar di menu Config.",
                    "File Tidak Ditemukan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                TxtFilePath.Text = iniPath;
                TxtPlaceholder.Visibility = Visibility.Collapsed;
                _main.SetIniPath(iniPath);
                Refresh();
                ShowStatus($"✔ CitizenFX.ini berhasil dimuat dari folder FiveM");
            }
            catch (Exception ex)
            {
                ShowStatus($"⚠ Gagal load CitizenFX.ini: {ex.Message}", false);
            }
        }

        private void TxtFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtPlaceholder.Visibility = string.IsNullOrEmpty(TxtFilePath.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private static IniData CloneData(IniData src) => new()
        {
            IVPath = src.IVPath,
            SavedBuildNumber = src.SavedBuildNumber,
            ReplaceExecutable = src.ReplaceExecutable,
            ReShade5 = src.ReShade5,
            PoolSizes = new System.Collections.Generic.Dictionary<string, int>(src.PoolSizes)
        };
    }
}
