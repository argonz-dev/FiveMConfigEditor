using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
                ? new SolidColorBrush(Color.FromRgb(0x23, 0x86, 0x36))
                : new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));
            StatusBarBorder.Visibility = string.IsNullOrEmpty(msg) ? Visibility.Collapsed : Visibility.Visible;

            _statusTimer?.Stop();
            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusTimer.Tick += (_, _) => { TxtStatus.Text = ""; StatusBarBorder.Visibility = Visibility.Collapsed; _statusTimer.Stop(); };
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

        private static Button CreateCardButton(IconChar icon, string text, Style style, int width)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(new IconBlock
            {
                Icon = icon,
                FontSize = 10,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            });
            return new Button
            {
                Content = sp,
                Width = width,
                Style = style,
                Margin = new Thickness(0, 0, 6, 4)
            };
        }

        private Border CreatePresetCard(Preset preset)
        {
            var card = new Border
            {
                Margin          = new Thickness(0, 0, 0, 8),
                CornerRadius    = new CornerRadius(8),
                Background      = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D)),
                BorderThickness = new Thickness(1),
                Cursor          = Cursors.Arrow,
                Effect          = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 8, ShadowDepth = 1, Opacity = 0.2
                }
            };
            card.MouseEnter += (_, _) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00));
            };
            card.MouseLeave += (_, _) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D));
            };

            var clipHost = new Grid { ClipToBounds = true };
            clipHost.SizeChanged += (_, _) =>
            {
                clipHost.Clip = new RectangleGeometry(
                    new Rect(0, 0, clipHost.ActualWidth, clipHost.ActualHeight), 7, 7);
            };
            var bannerImg = new System.Windows.Controls.Image
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.24,
                Visibility = Visibility.Collapsed
            };
            var bannerOverlay = new Border
            {
                Visibility = Visibility.Collapsed,
                Background = new LinearGradientBrush(
                    Color.FromArgb(0xEE, 0x16, 0x1B, 0x22),
                    Color.FromArgb(0x99, 0x16, 0x1B, 0x22),
                    new Point(0, 0.5),
                    new Point(1, 0.5))
            };
            if (!string.IsNullOrEmpty(preset.BannerPath) && File.Exists(preset.BannerPath))
            {
                var bannerSrc = ImageHelper.LoadImage(preset.BannerPath);
                if (bannerSrc != null)
                {
                    bannerImg.Source = bannerSrc;
                    bannerImg.Visibility = Visibility.Visible;
                    bannerOverlay.Visibility = Visibility.Visible;
                }
            }
            clipHost.Children.Add(bannerImg);
            clipHost.Children.Add(bannerOverlay);

            var outerGrid = new Grid { Margin = new Thickness(14, 12, 14, 12) };
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Thumbnail
            var thumbImg = new System.Windows.Controls.Image
            {
                Width = 44, Height = 44,
                Stretch = Stretch.UniformToFill,
                VerticalAlignment = VerticalAlignment.Center
            };
            var thumbBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Child = thumbImg,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            if (!string.IsNullOrEmpty(preset.ImagePath) && File.Exists(preset.ImagePath))
            {
                var imgSrc = ImageHelper.LoadImage(preset.ImagePath);
                if (imgSrc != null)
                {
                    thumbImg.Source = imgSrc;
                    thumbBorder.Visibility = Visibility.Visible;
                }
            }
            Grid.SetColumn(thumbBorder, 0);
            outerGrid.Children.Add(thumbBorder);

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(stack, 1);

            stack.Children.Add(new TextBlock
            {
                Text = preset.Name, Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC)),
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 3)
            });

            string desc = string.IsNullOrEmpty(preset.Description)
                ? $"Build: {preset.Data.SavedBuildNumber}  •  Pools: {preset.Data.PoolSizes.Count}"
                : preset.Description;
            stack.Children.Add(new TextBlock
            {
                Text = desc, Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 2)
            });

            bool hasServer = !string.IsNullOrWhiteSpace(preset.ServerUrl);
            bool hasNick = !string.IsNullOrWhiteSpace(preset.KeptNickname);
            if (hasServer || hasNick)
            {
                var metaParts = new List<string>();
                if (hasServer) metaParts.Add($"Server: {preset.ServerUrl.Trim()}");
                if (hasNick) metaParts.Add($"Nick: {preset.KeptNickname.Trim()}");
                stack.Children.Add(new TextBlock
                {
                    Text = string.Join("  •  ", metaParts),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 2)
                });
            }

            stack.Children.Add(new TextBlock
            {
                Text = $"Diperbarui: {preset.UpdatedAt:dd/MM/yyyy}",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x76, 0x81)),
                FontSize = 10, Margin = new Thickness(0, 0, 0, hasServer ? 3 : 8)
            });

            if (hasServer)
            {
                var txtServerStatus = new TextBlock
                {
                    Text = "Mengecek server...",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stack.Children.Add(txtServerStatus);

                bool probeStarted = false;
                card.Loaded += async (_, _) =>
                {
                    if (probeStarted) return;
                    probeStarted = true;

                    var infoTask = ServerProbeHelper.FetchDynamicAsync(preset.ServerUrl);
                    bool needIcon = string.IsNullOrWhiteSpace(preset.ImagePath) || !File.Exists(preset.ImagePath);
                    bool needBanner = string.IsNullOrWhiteSpace(preset.BannerPath) || !File.Exists(preset.BannerPath);
                    var assetsTask = (needIcon || needBanner)
                        ? ServerProbeHelper.FetchAndSaveServerAssetsAsync(preset.ServerUrl, preset.Id)
                        : Task.FromResult((false, (string?)null, (string?)null, ""));

                    try
                    {
                        var info = await infoTask;
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (!IsLoaded || !card.IsLoaded) return;
                            if (info.Ok)
                            {
                                var parts = new List<string> { $"{info.Clients}/{info.MaxClients} online" };
                                if (!string.IsNullOrWhiteSpace(info.GameBuild)) parts.Add($"Server Build {info.GameBuild}");
                                if (info.ResourceCount > 0) parts.Add($"{info.ResourceCount} resources");
                                if (!string.IsNullOrWhiteSpace(info.Hostname)) parts.Add(info.Hostname);
                                txtServerStatus.Text = string.Join("  •  ", parts);
                                txtServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
                            }
                            else
                            {
                                txtServerStatus.Text = "Offline / tidak terjangkau";
                                txtServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
                            }
                        });
                    }
                    catch
                    {
                        try
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (!IsLoaded || !card.IsLoaded) return;
                                txtServerStatus.Text = "Offline / tidak terjangkau";
                                txtServerStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
                            });
                        }
                        catch { }
                    }

                    if (needIcon || needBanner)
                    {
                        try
                        {
                            var (aOk, iconPath, bannerPath, _) = await assetsTask;
                            if (aOk)
                            {
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    bool updated = ApplyFetchedAssets(preset, iconPath, bannerPath, onlyIfMissing: true);
                                    if (updated) PresetManager.Save(AppState.Presets);
                                    if (!IsLoaded || !card.IsLoaded) return;

                                    if (!string.IsNullOrEmpty(preset.ImagePath) && File.Exists(preset.ImagePath))
                                    {
                                        var src = ImageHelper.LoadImage(preset.ImagePath);
                                        if (src != null)
                                        {
                                            thumbImg.Source = src;
                                            thumbBorder.Visibility = Visibility.Visible;
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(preset.BannerPath) && File.Exists(preset.BannerPath))
                                    {
                                        var bSrc = ImageHelper.LoadImage(preset.BannerPath);
                                        if (bSrc != null)
                                        {
                                            bannerImg.Source = bSrc;
                                            bannerImg.Visibility = Visibility.Visible;
                                            bannerOverlay.Visibility = Visibility.Visible;
                                        }
                                    }
                                });
                            }
                        }
                        catch { }
                    }
                };
            }

            var btnRow = new WrapPanel { Orientation = Orientation.Horizontal };

            var btnPlay = CreateCardButton(IconChar.Gamepad, "Play", (Style)Application.Current.FindResource("GreenButtonSmallStyle"), 68);
            btnPlay.Click += (_, _) => PlayPreset(preset);

            var btnApply = CreateCardButton(IconChar.Play, "Terapkan", (Style)Application.Current.FindResource("NeonButtonSmallStyle"), 86);
            btnApply.Click += (_, _) => ApplyPreset(preset);

            var btnConfig = CreateCardButton(IconChar.Sliders, "Config", (Style)Application.Current.FindResource("SecondaryButtonSmallStyle"), 74);
            btnConfig.Click += (_, _) => OpenPresetConfig(preset);

            var btnEdit = CreateCardButton(IconChar.PenToSquare, "Edit", (Style)Application.Current.FindResource("SecondaryButtonSmallStyle"), 66);
            btnEdit.Click += (_, _) => EditPreset(preset);

            var btnDelete = CreateCardButton(IconChar.TrashCan, "Hapus", (Style)Application.Current.FindResource("RedButtonSmallStyle"), 72);
            btnDelete.Click += (_, _) => DeletePreset(preset);

            btnRow.Children.Add(btnPlay);
            btnRow.Children.Add(btnApply);
            btnRow.Children.Add(btnConfig);
            btnRow.Children.Add(btnEdit);
            btnRow.Children.Add(btnDelete);

            if (hasServer)
            {
                var btnPing = CreateCardButton(IconChar.Wifi, "Ping", (Style)Application.Current.FindResource("SecondaryButtonSmallStyle"), 66);
                btnPing.Click += async (_, _) => await PingPresetServerAsync(preset, btnPing);
                btnRow.Children.Add(btnPing);
            }

            stack.Children.Add(btnRow);

            outerGrid.Children.Add(stack);
            clipHost.Children.Add(outerGrid);
            card.Child = clipHost;
            return card;
        }

        private bool EnsureIniPath()
        {
            if (string.IsNullOrEmpty(AppState.IniPath) && !string.IsNullOrEmpty(AppState.FiveMPath))
            {
                var candidate = Path.Combine(AppState.FiveMPath, "CitizenFX.ini");
                if (File.Exists(candidate))
                {
                    TxtFilePath.Text = candidate;
                    TxtPlaceholder.Visibility = Visibility.Collapsed;
                    _main.SetIniPath(candidate);
                }
            }
            if (!string.IsNullOrEmpty(AppState.IniPath)) return true;
            ShowStatus("⚠ Pilih file CitizenFX.ini terlebih dahulu!", false);
            return false;
        }

        private bool ApplyPreset(Preset preset, bool showStatus = true)
        {
            if (!EnsureIniPath()) return false;

            try
            {
                AppState.Data = CloneData(preset.Data);
                _main.SuppressWatcherUntil(TimeSpan.FromMilliseconds(800));
                bool changed = IniHelper.Save(AppState.IniPath, AppState.Data);
                if (changed)
                {
                    SnapshotManager.Add(AppState.Snapshots, new Snapshot
                    {
                        Label = $"Terapkan: {preset.Name}",
                        SourceFile = AppState.IniPath,
                        Data = CloneData(preset.Data),
                        CapturedAt = DateTime.Now
                    });
                }
                if (showStatus)
                    ShowStatus($"✔ Preset \"{preset.Name}\" berhasil diterapkan ke {Path.GetFileName(AppState.IniPath)}");
                return changed;
            }
            catch (Exception ex)
            {
                ShowStatus($"⚠ Gagal menerapkan preset \"{preset.Name}\": {ex.Message}", false);
                return false;
            }
        }

        private async Task PingPresetServerAsync(Preset preset, Button btnPing)
        {
            btnPing.IsEnabled = false;
            ShowStatus($"Mengukur ping {preset.Name}...");
            try
            {
                var progress = new Progress<(int Done, int Total, double? LastMs)>(p =>
                {
                    if (!IsLoaded || !btnPing.IsLoaded) return;
                    string sample = p.LastMs.HasValue ? $"{p.LastMs.Value:F0} ms" : "timeout";
                    ShowStatus($"Ping {preset.Name} ({p.Done}/{p.Total}): {sample}");
                });

                var r = await ServerProbeHelper.ProbeNetworkAsync(preset.ServerUrl, rounds: 8, progress: progress);
                if (!IsLoaded || !btnPing.IsLoaded) return;

                if (r.Ok)
                    ShowStatus($"✔ {preset.Name}: {r.AvgMs:F0} ms | Jitter {r.JitterMs:F1} ms | Loss {r.LossPercent:F0}% ({r.Rating})");
                else
                    ShowStatus($"⚠ {preset.Name}: {(string.IsNullOrWhiteSpace(r.Error) ? r.Rating : r.Error)}", false);
            }
            catch (Exception ex)
            {
                if (IsLoaded && btnPing.IsLoaded)
                    ShowStatus($"⚠ Gagal ping {preset.Name}: {ex.Message}", false);
            }
            finally
            {
                if (btnPing.IsLoaded)
                    btnPing.IsEnabled = true;
            }
        }

        private async void PlayPreset(Preset preset)
        {
            if (!EnsureIniPath()) return;

            bool changed = ApplyPreset(preset, showStatus: false);
            string? nickNote = null;
            if (!string.IsNullOrWhiteSpace(preset.KeptNickname))
            {
                try
                {
                    var (nickOk, nickMsg) = FiveMCfgHelper.WriteNickname(preset.KeptNickname);
                    if (!nickOk)
                        nickNote = $"Nickname gagal diset: {nickMsg}";
                }
                catch (Exception ex)
                {
                    nickNote = $"Nickname gagal diset: {ex.Message}";
                }
            }
            await LaunchFiveMWithCheckAsync(changed, preset.ServerUrl, preset.Name, nickNote);
            if (!string.IsNullOrWhiteSpace(preset.ServerUrl))
                _ = RefreshServerAssetsAfterConnectAsync(preset);
        }

        private static bool ApplyFetchedAssets(Preset preset, string? iconPath, string? bannerPath, bool onlyIfMissing)
        {
            bool changed = false;
            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_assets");

            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                bool isAutoOwned = string.IsNullOrWhiteSpace(preset.ImagePath) ||
                                   !File.Exists(preset.ImagePath) ||
                                   preset.ImagePath.StartsWith(assetsDir, StringComparison.OrdinalIgnoreCase);
                if (!onlyIfMissing ? isAutoOwned : (string.IsNullOrWhiteSpace(preset.ImagePath) || !File.Exists(preset.ImagePath)))
                {
                    if (!string.Equals(preset.ImagePath, iconPath, StringComparison.OrdinalIgnoreCase))
                        changed = true;
                    preset.ImagePath = iconPath;
                }
            }

            if (!string.IsNullOrEmpty(bannerPath) && File.Exists(bannerPath))
            {
                if (!onlyIfMissing || string.IsNullOrWhiteSpace(preset.BannerPath) || !File.Exists(preset.BannerPath))
                {
                    if (!string.Equals(preset.BannerPath, bannerPath, StringComparison.OrdinalIgnoreCase))
                        changed = true;
                    preset.BannerPath = bannerPath;
                }
            }
            return changed;
        }

        private async Task RefreshServerAssetsAfterConnectAsync(Preset preset)
        {
            try
            {
                var (ok, iconPath, bannerPath, _) = await ServerProbeHelper.FetchAndSaveServerAssetsAsync(preset.ServerUrl, preset.Id);
                if (!ok) return;

                await Dispatcher.InvokeAsync(() =>
                {
                    ApplyFetchedAssets(preset, iconPath, bannerPath, onlyIfMissing: false);
                    PresetManager.Save(AppState.Presets);
                    if (IsLoaded) RenderPresets();
                });
            }
            catch { }
        }

        private async void BtnPlayFiveM_Click(object sender, RoutedEventArgs e)
        {
            await LaunchFiveMWithCheckAsync(configChanged: false, serverUrl: null, label: null);
        }

        private async Task LaunchFiveMWithCheckAsync(bool configChanged, string? serverUrl, string? label, string? note = null)
        {
            if (configChanged && FiveMLauncher.IsFiveMRunning())
            {
                var ans = MessageBox.Show(
                    "Konfigurasi baru sudah diterapkan ke CitizenFX.ini, tetapi FiveM sedang berjalan.\n" +
                    "FiveM hanya membaca CitizenFX.ini saat startup.\n\n" +
                    "Tutup FiveM sekarang lalu jalankan ulang?",
                    "Restart FiveM Diperlukan",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (ans != MessageBoxResult.Yes)
                {
                    ShowStatus("⚠ Tutup FiveM sepenuhnya lalu klik Play lagi agar konfigurasi baru aktif.", false);
                    return;
                }
                await FiveMLauncher.CloseRunningFiveMAsync();
            }

            var (ok, msg) = FiveMLauncher.Launch(serverUrl);
            string suffix = string.IsNullOrEmpty(note) ? "" : $" (⚠ {note})";
            if (ok && !string.IsNullOrEmpty(label))
                ShowStatus($"✔ Preset \"{label}\" diterapkan — {msg}{suffix}");
            else
                ShowStatus(ok ? $"✔ {msg}{suffix}" : $"⚠ {msg}{suffix}", ok);
        }

        private void OpenPresetConfig(Preset preset)
        {
            var dlg = new PresetConfigDialog(preset) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
            {
                preset.UpdatedAt = DateTime.Now;
                PresetManager.Save(AppState.Presets);
                RenderPresets();
                if (dlg.ApplyAfterSave)
                    ApplyPreset(preset);
                else
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
            DefaultBuild = src.DefaultBuild,
            ReplaceExecutable = src.ReplaceExecutable,
            ReShade5 = src.ReShade5,
            UpdateChannel = src.UpdateChannel,
            PoolSizes = new System.Collections.Generic.Dictionary<string, int>(src.PoolSizes)
        };
    }
}
