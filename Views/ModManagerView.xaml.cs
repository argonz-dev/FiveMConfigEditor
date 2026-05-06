using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Views
{
    public partial class ModManagerView : UserControl
    {
        private readonly MainWindow _main;
        private List<ModItem> _mods    = new();
        private List<ModItem> _plugins = new();
        private List<ModsPreset> _graphicsPresets = new();
        private DispatcherTimer? _statusTimer;
        private string _activeTab = "Mods";

        public ModManagerView(MainWindow main)
        {
            _main = main;
            InitializeComponent();
        }

        public void Refresh()
        {
            _graphicsPresets = ModsPresetManager.Load();
            LoadMods();
            LoadPlugins();
            RenderActiveTab();
        }

        private void LoadMods()
        {
            try { _mods = ModManager.ScanMods(); }
            catch (Exception ex) { ShowStatus($"Error scan mods: {ex.Message}", false); _mods = new(); }
        }

        private void LoadPlugins()
        {
            try { _plugins = ModManager.ScanPlugins(); }
            catch (Exception ex) { ShowStatus($"Error scan plugins: {ex.Message}", false); _plugins = new(); }
        }

        // ── Tab Navigation ─────────────────────────────────────────────────────
        private void TabMods_Click(object sender, RoutedEventArgs e)    => SwitchTab("Mods");
        private void TabPlugins_Click(object sender, RoutedEventArgs e) => SwitchTab("Plugins");
        private void TabPresets_Click(object sender, RoutedEventArgs e) => SwitchTab("Presets");

        private void SwitchTab(string tab)
        {
            _activeTab = tab;
            TabMods.Style    = tab == "Mods"    ? (Style)FindResource("TabBtnActiveStyle") : (Style)FindResource("TabBtnStyle");
            TabPlugins.Style = tab == "Plugins" ? (Style)FindResource("TabBtnActiveStyle") : (Style)FindResource("TabBtnStyle");
            TabPresets.Style = tab == "Presets" ? (Style)FindResource("TabBtnActiveStyle") : (Style)FindResource("TabBtnStyle");

            PanelMods.Visibility    = tab == "Mods"    ? Visibility.Visible : Visibility.Collapsed;
            PanelPlugins.Visibility = tab == "Plugins" ? Visibility.Visible : Visibility.Collapsed;
            PanelPresets.Visibility = tab == "Presets" ? Visibility.Visible : Visibility.Collapsed;

            RenderActiveTab();
        }

        private void RenderActiveTab()
        {
            switch (_activeTab)
            {
                case "Mods":    RenderMods();    break;
                case "Plugins": RenderPlugins(); break;
                case "Presets": RenderGraphicsPresets(); break;
            }
        }

        // ── Render Mods ────────────────────────────────────────────────────────
        private void RenderMods()
        {
            ModsList.Children.Clear();
            int enabled  = _mods.Count(m => m.IsEnabled);
            int disabled = _mods.Count - enabled;
            TxtModsStats.Text = $"{_mods.Count} mod ditemukan  •  {enabled} aktif  •  {disabled} nonaktif";

            if (_mods.Count == 0)
            {
                ModsList.Children.Add(EmptyLabel("Tidak ada mod RPF ditemukan di folder mods."));
                return;
            }

            foreach (var mod in _mods)
                ModsList.Children.Add(CreateItemCard(mod, isPlugin: false));
        }

        // ── Render Plugins ─────────────────────────────────────────────────────
        private void RenderPlugins()
        {
            PluginsList.Children.Clear();
            int enabled  = _plugins.Count(p => p.IsEnabled);
            int disabled = _plugins.Count - enabled;
            TxtPluginsStats.Text = $"{_plugins.Count} plugin ditemukan  •  {enabled} aktif  •  {disabled} nonaktif";

            if (_plugins.Count == 0)
            {
                PluginsList.Children.Add(EmptyLabel("Tidak ada plugin ditemukan di folder plugins."));
                return;
            }

            // Group by type
            var groups = _plugins.GroupBy(p => p.Type).OrderBy(g => g.Key.ToString());
            foreach (var group in groups)
            {
                PluginsList.Children.Add(GroupHeader(group.Key.ToString()));
                foreach (var plugin in group)
                    PluginsList.Children.Add(CreateItemCard(plugin, isPlugin: true));
            }
        }

        // ── Render Graphics Presets ────────────────────────────────────────────
        private void RenderGraphicsPresets()
        {
            PresetsList.Children.Clear();

            if (_graphicsPresets.Count == 0)
            {
                PresetsList.Children.Add(EmptyLabel("Belum ada graphics preset. Klik \"💾 Simpan Preset\" untuk membuat."));
                return;
            }

            foreach (var preset in _graphicsPresets)
                PresetsList.Children.Add(CreatePresetCard(preset));
        }

        // ── Item Card ──────────────────────────────────────────────────────────
        private Border CreateItemCard(ModItem item, bool isPlugin)
        {
            var card = new Border
            {
                Background    = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                CornerRadius  = new CornerRadius(8),
                BorderBrush   = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x4A)),
                BorderThickness = new Thickness(1),
                Margin        = new Thickness(0, 0, 0, 6),
                Padding       = new Thickness(14, 10, 14, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: icon + info
            var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Type icon
            var icon = new TextBlock
            {
                Text       = GetTypeIcon(item.Type),
                FontSize   = 18,
                Margin     = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            left.Children.Add(icon);

            var info = new StackPanel();
            info.Children.Add(new TextBlock
            {
                Text       = item.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold
            });

            var meta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            meta.Children.Add(MakeTag(item.TypeLabel, Color.FromRgb(0x00, 0x78, 0xD4)));
            meta.Children.Add(MakeTag(item.SizeDisplay, Color.FromRgb(0x44, 0x44, 0x66)));
            if (item.IsDirectory) meta.Children.Add(MakeTag("📁 Folder", Color.FromRgb(0x44, 0x44, 0x66)));
            info.Children.Add(meta);

            left.Children.Add(info);
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            // Right: status + toggle button
            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var statusDot = new System.Windows.Shapes.Ellipse
            {
                Width  = 8, Height = 8,
                Fill   = item.IsEnabled
                    ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                    : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            right.Children.Add(statusDot);

            var statusTxt = new TextBlock
            {
                Text       = item.IsEnabled ? "Aktif" : "Nonaktif",
                Foreground = item.IsEnabled
                    ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                    : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize   = 11,
                Margin     = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            right.Children.Add(statusTxt);

            var toggleBtn = new Button
            {
                Content    = item.IsEnabled ? "Nonaktifkan" : "Aktifkan",
                Background = item.IsEnabled
                    ? new SolidColorBrush(Color.FromRgb(0x4A, 0x1A, 0x1A))
                    : new SolidColorBrush(Color.FromRgb(0x1A, 0x4A, 0x2A)),
                Foreground = new SolidColorBrush(Colors.White),
                Style      = (Style)FindResource("ToggleEnableStyle"),
                Tag        = (item, isPlugin)
            };
            toggleBtn.Click += ToggleItem_Click;
            right.Children.Add(toggleBtn);

            Grid.SetColumn(right, 1);
            grid.Children.Add(right);

            card.Child = grid;
            return card;
        }

        private void ToggleItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var (item, isPlugin) = ((ModItem, bool))btn.Tag;

            try
            {
                if (isPlugin)
                    ModManager.SetPluginEnabled(item, !item.IsEnabled);
                else
                    ModManager.SetModEnabled(item, !item.IsEnabled);

                ShowStatus($"{item.Name} → {(item.IsEnabled ? "Aktif" : "Nonaktif")}");
                RenderActiveTab();
            }
            catch (Exception ex)
            {
                ShowStatus($"Gagal: {ex.Message}", false);
            }
        }

        // ── Graphics Preset Card ───────────────────────────────────────────────
        private Border CreatePresetCard(ModsPreset preset)
        {
            var card = new Border
            {
                Background    = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                CornerRadius  = new CornerRadius(8),
                BorderBrush   = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x4A)),
                BorderThickness = new Thickness(1),
                Margin        = new Thickness(0, 0, 0, 8),
                Padding       = new Thickness(16, 12, 16, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel();
            info.Children.Add(new TextBlock
            {
                Text       = preset.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                FontSize   = 13, FontWeight = FontWeights.SemiBold
            });
            if (!string.IsNullOrEmpty(preset.Description))
                info.Children.Add(new TextBlock
                {
                    Text       = preset.Description,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    FontSize   = 11, Margin = new Thickness(0, 2, 0, 0)
                });

            var meta = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            meta.Children.Add(MakeTag($"📦 {preset.ModsState.Count(kv => kv.Value)} mods aktif", Color.FromRgb(0x00, 0x78, 0xD4)));
            meta.Children.Add(MakeTag($"🔌 {preset.PluginsState.Count(kv => kv.Value)} plugins aktif", Color.FromRgb(0x44, 0x44, 0x88)));
            meta.Children.Add(MakeTag(preset.CreatedAt.ToString("dd/MM/yyyy"), Color.FromRgb(0x33, 0x33, 0x55)));
            info.Children.Add(meta);

            Grid.SetColumn(info, 0);
            grid.Children.Add(info);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var applyBtn = new Button
            {
                Content    = "▶ Terapkan",
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                Foreground = new SolidColorBrush(Colors.White),
                Style      = (Style)FindResource("SmallActionBtn"),
                Tag        = preset,
                Margin     = new Thickness(0, 0, 6, 0)
            };
            applyBtn.Click += ApplyPreset_Click;
            actions.Children.Add(applyBtn);

            var deleteBtn = new Button
            {
                Content    = "🗑",
                Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x1A, 0x1A)),
                Foreground = new SolidColorBrush(Colors.White),
                Style      = (Style)FindResource("SmallActionBtn"),
                Tag        = preset
            };
            deleteBtn.Click += DeletePreset_Click;
            actions.Children.Add(deleteBtn);

            Grid.SetColumn(actions, 1);
            grid.Children.Add(actions);

            card.Child = grid;
            return card;
        }

        private void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ModsPreset preset) return;
            try
            {
                ModManager.ApplyGraphicsPreset(preset);
                LoadMods();
                LoadPlugins();
                RenderActiveTab();
                ShowStatus($"Preset \"{preset.Name}\" diterapkan.");
            }
            catch (Exception ex) { ShowStatus($"Gagal terapkan preset: {ex.Message}", false); }
        }

        private void DeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ModsPreset preset) return;
            var result = MessageBox.Show($"Hapus preset \"{preset.Name}\"?", "Konfirmasi",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            _graphicsPresets.Remove(preset);
            ModsPresetManager.Save(_graphicsPresets);
            RenderGraphicsPresets();
            ShowStatus($"Preset \"{preset.Name}\" dihapus.");
        }

        // ── Toolbar Buttons ────────────────────────────────────────────────────
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
            ShowStatus("Folder di-refresh.");
        }

        private void BtnExportPack_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.ExportModPackDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            var saveDialog = new SaveFileDialog
            {
                Filter = "FiveM Mod Pack (*.fmpack)|*.fmpack",
                FileName = $"{dialog.PackName}.fmpack",
                Title = "Simpan Mod Pack"
            };

            if (saveDialog.ShowDialog() != true) return;

            // Show progress dialog
            var progressDialog = new Dialogs.ProgressDialog("Export Mod Pack", "Mengumpulkan file...")
            {
                Owner = Window.GetWindow(this)
            };

            Task.Run(() =>
            {
                try
                {
                    progressDialog.Dispatcher.Invoke(() => progressDialog.Show());
                    
                    progressDialog.UpdateStatus("Menyalin mods dan plugins...");
                    ModPackManager.ExportPack(saveDialog.FileName, dialog.PackName, dialog.PackDescription, dialog.PackAuthor);
                    
                    progressDialog.UpdateStatus("Membuat file ZIP...");
                    System.Threading.Thread.Sleep(500); // Brief pause for user feedback

                    progressDialog.Dispatcher.Invoke(() => progressDialog.Close());

                    Dispatcher.Invoke(() =>
                    {
                        ShowStatus($"Mod pack \"{dialog.PackName}\" berhasil di-export.");
                        MessageBox.Show($"Mod pack berhasil disimpan ke:\n{saveDialog.FileName}", "Sukses",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    progressDialog.Dispatcher.Invoke(() => progressDialog.Close());
                    Dispatcher.Invoke(() =>
                    {
                        ShowStatus($"Gagal export: {ex.Message}", false);
                        MessageBox.Show($"Gagal export mod pack:\n{ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void BtnImportPack_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "FiveM Mod Pack (*.fmpack)|*.fmpack",
                Title = "Pilih Mod Pack untuk Import"
            };

            if (openDialog.ShowDialog() != true) return;

            try
            {
                // Read metadata first
                var metadata = ModPackManager.ReadPackMetadata(openDialog.FileName);
                if (metadata == null)
                {
                    MessageBox.Show("File mod pack tidak valid.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Show confirmation dialog with backup option
                var confirmDialog = new Dialogs.ImportPackConfirmDialog(metadata)
                {
                    Owner = Window.GetWindow(this)
                };

                if (confirmDialog.ShowDialog() != true) return;

                // Create backup if requested
                if (confirmDialog.CreateBackup)
                {
                    try
                    {
                        var backupPreset = ModManager.CaptureCurrentState(
                            confirmDialog.BackupName,
                            "Backup otomatis sebelum import mod pack");
                        _graphicsPresets.Insert(0, backupPreset);
                        ModsPresetManager.Save(_graphicsPresets);
                        ShowStatus($"Backup preset \"{backupPreset.Name}\" dibuat.");
                    }
                    catch (Exception ex)
                    {
                        var result = MessageBox.Show(
                            $"Gagal membuat backup:\n{ex.Message}\n\nLanjutkan import tanpa backup?",
                            "Warning",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        if (result != MessageBoxResult.Yes) return;
                    }
                }

                // Show progress dialog
                var progressDialog = new Dialogs.ProgressDialog("Import Mod Pack", "Mengekstrak file...")
                {
                    Owner = Window.GetWindow(this)
                };

                Task.Run(() =>
                {
                    try
                    {
                        progressDialog.Dispatcher.Invoke(() => progressDialog.Show());

                        progressDialog.UpdateStatus("Mengekstrak ZIP...");
                        System.Threading.Thread.Sleep(300);

                        progressDialog.UpdateStatus("Menyalin ke folder mods dan plugins...");
                        ModPackManager.ImportPack(openDialog.FileName, overwrite: true);

                        progressDialog.UpdateStatus("Selesai!");
                        System.Threading.Thread.Sleep(500);

                        progressDialog.Dispatcher.Invoke(() => progressDialog.Close());

                        Dispatcher.Invoke(() =>
                        {
                            LoadMods();
                            LoadPlugins();
                            RenderActiveTab();
                            ShowStatus($"Mod pack \"{metadata.Name}\" berhasil di-import.");
                            MessageBox.Show($"Mod pack \"{metadata.Name}\" berhasil di-import.", "Sukses",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }
                    catch (Exception ex)
                    {
                        progressDialog.Dispatcher.Invoke(() => progressDialog.Close());
                        Dispatcher.Invoke(() =>
                        {
                            ShowStatus($"Gagal import: {ex.Message}", false);
                            MessageBox.Show($"Gagal import mod pack:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                ShowStatus($"Gagal import: {ex.Message}", false);
                MessageBox.Show($"Gagal import mod pack:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.SaveGraphicsPresetDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var preset = ModManager.CaptureCurrentState(dialog.PresetName, dialog.PresetDescription);
                _graphicsPresets.Insert(0, preset);
                ModsPresetManager.Save(_graphicsPresets);
                ShowStatus($"Preset \"{preset.Name}\" disimpan.");
                if (_activeTab == "Presets") RenderGraphicsPresets();
            }
            catch (Exception ex) { ShowStatus($"Gagal simpan preset: {ex.Message}", false); }
        }

        private void BtnEnableAllMods_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in _mods.Where(m => !m.IsEnabled))
                try { ModManager.SetModEnabled(m, true); } catch { }
            LoadMods(); RenderMods();
            ShowStatus("Semua mod diaktifkan.");
        }

        private void BtnDisableAllMods_Click(object sender, RoutedEventArgs e)
        {
            foreach (var m in _mods.Where(m => m.IsEnabled))
                try { ModManager.SetModEnabled(m, false); } catch { }
            LoadMods(); RenderMods();
            ShowStatus("Semua mod dinonaktifkan.");
        }

        private void BtnEnableAllPlugins_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in _plugins.Where(p => !p.IsEnabled))
                try { ModManager.SetPluginEnabled(p, true); } catch { }
            LoadPlugins(); RenderPlugins();
            ShowStatus("Semua plugin diaktifkan.");
        }

        private void BtnDisableAllPlugins_Click(object sender, RoutedEventArgs e)
        {
            foreach (var p in _plugins.Where(p => p.IsEnabled))
                try { ModManager.SetPluginEnabled(p, false); } catch { }
            LoadPlugins(); RenderPlugins();
            ShowStatus("Semua plugin dinonaktifkan.");
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static string GetTypeIcon(ModType type) => type switch
        {
            ModType.Rpf           => "📦",
            ModType.Plugin        => "🔌",
            ModType.Upscaler      => "🔭",
            ModType.ReShadeHook   => "🎨",
            ModType.ReShadeShaders => "✨",
            ModType.QuantVAddon   => "🌟",
            _ => "📄"
        };

        private static Border MakeTag(string text, Color bg)
        {
            return new Border
            {
                Background    = new SolidColorBrush(Color.FromArgb(0x33, bg.R, bg.G, bg.B)),
                CornerRadius  = new CornerRadius(4),
                Padding       = new Thickness(6, 2, 6, 2),
                Margin        = new Thickness(0, 0, 4, 0),
                Child         = new TextBlock
                {
                    Text       = text,
                    Foreground = new SolidColorBrush(Color.FromRgb(
                        (byte)Math.Min(bg.R + 80, 255),
                        (byte)Math.Min(bg.G + 80, 255),
                        (byte)Math.Min(bg.B + 80, 255))),
                    FontSize   = 10
                }
            };
        }

        private static Border GroupHeader(string title)
        {
            return new Border
            {
                Padding = new Thickness(0, 8, 0, 4),
                Child   = new TextBlock
                {
                    Text       = title,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                    FontSize   = 11,
                    FontWeight = FontWeights.Bold
                }
            };
        }

        private static TextBlock EmptyLabel(string msg) => new()
        {
            Text                = msg,
            Foreground          = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88)),
            FontSize            = 12,
            Margin              = new Thickness(0, 20, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

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
    }
}
