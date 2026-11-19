using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FontAwesome.Sharp;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Views
{
    public sealed class KeybindRow
    {
        public bool IsActive { get; set; } = true;
        public string Mapper { get; set; } = "KEYBOARD";
        public string Key { get; set; } = "";
        public string Command { get; set; } = "";
    }

    public partial class FiveMToolsView : UserControl
    {
        private enum BannerTone { Success, Error, Info }

        private readonly MainWindow _main;
        private readonly ObservableCollection<KeybindRow> _keybinds = new();
        private string _detectedNuiNick = "";
        private CancellationTokenSource? _scanCts;
        private bool _isScanningCache;
        private bool _isCacheBusy;

        public FiveMToolsView(MainWindow main)
        {
            _main = main;
            InitializeComponent();
            KeybindsItems.ItemsSource = _keybinds;
            Refresh();
        }

        public void Refresh()
        {
            LoadNicknameSection();
            LoadKeybindsSection();
            _ = RefreshCacheAsync(preserveBanner: false);
        }

        // ── Nickname Section ───────────────────────────────────────────────

        private void LoadNicknameSection()
        {
            string cfgPath = FiveMCfgHelper.GetFiveMCfgPath();
            bool cfgExists = File.Exists(cfgPath);
            TxtCfgStatus.Text = cfgExists
                ? $"fivem.cfg ditemukan ({cfgPath})"
                : $"fivem.cfg belum ada (akan dibuat saat disimpan)";

            var (nick, _, err) = FiveMCfgHelper.ReadNickname();
            TxtNickname.Text = nick;

            if (!string.IsNullOrEmpty(err))
                ShowBanner(NicknameBanner, IconNicknameBanner, TxtNicknameStatus, $"Gagal membaca fivem.cfg: {err}", BannerTone.Error);
            else
                NicknameBanner.Visibility = Visibility.Collapsed;

            var (nuiVal, sourceDir, nuiFound) = FiveMCfgHelper.ReadNuiNickOverride(AppState.FiveMPath);
            _detectedNuiNick = nuiFound ? nuiVal : "";
            BtnUseNuiNick.IsEnabled = nuiFound && !string.IsNullOrEmpty(nuiVal);

            if (nuiFound && !string.IsNullOrEmpty(nuiVal))
            {
                TxtNuiNick.Text = $"NUI nickOverride terdeteksi: {nuiVal}";
                if (!string.IsNullOrEmpty(sourceDir))
                {
                    TxtNuiSource.Text = $"Sumber: {sourceDir}";
                    TxtNuiSource.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtNuiSource.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                TxtNuiNick.Text = string.IsNullOrWhiteSpace(AppState.FiveMPath)
                    ? "NUI nickOverride: Path FiveM.app belum diatur."
                    : "NUI nickOverride tidak ditemukan di data\\nui-storage.";
                TxtNuiSource.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnUseNuiNick_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_detectedNuiNick)) return;
            TxtNickname.Text = _detectedNuiNick;
            ShowBanner(NicknameBanner, IconNicknameBanner, TxtNicknameStatus,
                $"Nilai NUI '{_detectedNuiNick}' diisikan. Klik 'Simpan Nickname' untuk menyimpan ke fivem.cfg.",
                BannerTone.Info);
        }

        private void BtnSaveNickname_Click(object sender, RoutedEventArgs e)
        {
            var (ok, message) = FiveMCfgHelper.WriteNickname(TxtNickname.Text);
            ShowBanner(NicknameBanner, IconNicknameBanner, TxtNicknameStatus, message,
                ok ? BannerTone.Success : BannerTone.Error);

            if (ok)
            {
                string cfgPath = FiveMCfgHelper.GetFiveMCfgPath();
                TxtCfgStatus.Text = File.Exists(cfgPath)
                    ? $"fivem.cfg ditemukan ({cfgPath})"
                    : "fivem.cfg belum ada (akan dibuat saat disimpan)";
            }
        }

        // ── Keybind Section ────────────────────────────────────────────────

        private void LoadKeybindsSection(bool clearBanner = true)
        {
            var (items, err) = FiveMCfgHelper.ReadKeybinds();
            _keybinds.Clear();
            foreach (var item in items)
            {
                _keybinds.Add(new KeybindRow
                {
                    IsActive = !item.Disabled,
                    Mapper = item.Mapper,
                    Key = item.Key,
                    Command = item.Command
                });
            }

            UpdateKeybindSummary();

            if (!string.IsNullOrEmpty(err))
                ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus, $"Sebagian data gagal dibaca: {err}", BannerTone.Error);
            else if (clearBanner)
                KeybindBanner.Visibility = Visibility.Collapsed;
        }

        private void UpdateKeybindSummary()
        {
            int activeCount = _keybinds.Count(k => k.IsActive);
            int disabledCount = _keybinds.Count - activeCount;
            TxtKeybindCount.Text = $"{_keybinds.Count} keybind ({activeCount} aktif, {disabledCount} nonaktif)";
            EmptyKeybindsBox.Visibility = _keybinds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAddKeybind_Click(object sender, RoutedEventArgs e)
        {
            string mapper = (CmbNewMapper.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "KEYBOARD";
            string key = TxtNewKey.Text.Trim();
            string cmd = TxtNewCommand.Text.Trim();

            if (string.IsNullOrEmpty(key))
            {
                ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus, "Key tidak boleh kosong (contoh: F5).", BannerTone.Error);
                TxtNewKey.Focus();
                return;
            }
            if (string.IsNullOrEmpty(cmd))
            {
                ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus, "Command tidak boleh kosong (contoh: e dance).", BannerTone.Error);
                TxtNewCommand.Focus();
                return;
            }

            bool duplicateActive = _keybinds.Any(k =>
                k.IsActive &&
                k.Mapper.Trim().Equals(mapper, StringComparison.OrdinalIgnoreCase) &&
                k.Key.Trim().Equals(key, StringComparison.OrdinalIgnoreCase));

            if (duplicateActive)
            {
                ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus,
                    $"Keybind aktif untuk {mapper} {key} sudah ada di daftar.", BannerTone.Error);
                return;
            }

            _keybinds.Add(new KeybindRow
            {
                IsActive = true,
                Mapper = mapper,
                Key = key,
                Command = cmd
            });

            TxtNewKey.Clear();
            TxtNewCommand.Clear();
            UpdateKeybindSummary();
            ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus,
                $"Keybind '{mapper} {key}' ditambahkan. Klik 'Simpan Keybinds' untuk menulis ke fivem.cfg.",
                BannerTone.Info);
        }

        private void ChkKeybindActive_Click(object sender, RoutedEventArgs e)
            => UpdateKeybindSummary();

        private void BtnDeleteKeybind_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not KeybindRow row) return;

            _keybinds.Remove(row);
            UpdateKeybindSummary();
            ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus,
                "Baris dihapus dari daftar. Klik 'Simpan Keybinds' untuk menyimpan perubahan.",
                BannerTone.Info);
        }

        private void BtnReloadKeybinds_Click(object sender, RoutedEventArgs e)
        {
            LoadKeybindsSection(clearBanner: false);
            ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus,
                "Daftar keybind dimuat ulang dari fivem.cfg.",
                BannerTone.Info);
        }

        private void BtnSaveKeybinds_Click(object sender, RoutedEventArgs e)
        {
            var toSave = _keybinds.Select(r => new FiveMKeybind
            {
                Mapper = r.Mapper,
                Key = r.Key,
                Command = r.Command,
                Disabled = !r.IsActive
            }).ToList();

            var (ok, message) = FiveMCfgHelper.SaveKeybinds(toSave);
            if (ok)
                LoadKeybindsSection(clearBanner: false);

            ShowBanner(KeybindBanner, IconKeybindBanner, TxtKeybindStatus, message,
                ok ? BannerTone.Success : BannerTone.Error);
        }

        // ── Cache Manager Section ──────────────────────────────────────────

        private async Task RefreshCacheAsync(bool preserveBanner)
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            var cts = new CancellationTokenSource();
            _scanCts = cts;

            if (string.IsNullOrWhiteSpace(AppState.FiveMPath) || !Directory.Exists(AppState.FiveMPath))
            {
                _isScanningCache = false;
                UpdateCacheButtonsEnabled();
                CacheItems.ItemsSource = FiveMCacheHelper.GetCacheFolders("");
                TxtCacheSummary.Text = "Folder FiveM.app belum valid.";
                if (!preserveBanner)
                {
                    ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus,
                        "Direktori FiveM.app tidak ditemukan. Atur folder FiveM di menu Config terlebih dahulu.",
                        BannerTone.Error);
                }
                return;
            }

            _isScanningCache = true;
            UpdateCacheButtonsEnabled();
            TxtCacheSummary.Text = $"Memindai {Path.Combine(AppState.FiveMPath, "data")}...";
            if (!preserveBanner)
                CacheBanner.Visibility = Visibility.Collapsed;

            try
            {
                var folders = await FiveMCacheHelper.ScanCacheFoldersAsync(AppState.FiveMPath, cts.Token);
                if (cts.IsCancellationRequested || !ReferenceEquals(_scanCts, cts))
                    return;

                CacheItems.ItemsSource = folders;
                long totalBytes = folders.Sum(f => f.SizeBytes);
                int totalFiles = folders.Sum(f => f.FileCount);
                TxtCacheSummary.Text = $"Total: {FiveMCacheHelper.FormatBytes(totalBytes)} ({totalFiles} file) di {Path.Combine(AppState.FiveMPath, "data")}";
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer Refresh call.
            }
            catch (Exception ex)
            {
                if (!preserveBanner)
                {
                    ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus,
                        $"Gagal memindai cache: {ex.Message}", BannerTone.Error);
                }
            }
            finally
            {
                if (ReferenceEquals(_scanCts, cts))
                {
                    _isScanningCache = false;
                    UpdateCacheButtonsEnabled();
                }
            }
        }

        private void UpdateCacheButtonsEnabled()
        {
            bool enabled = !_isScanningCache && !_isCacheBusy;
            BtnScanCache.IsEnabled = enabled;
            BtnInstallCache.IsEnabled = enabled;
        }

        private async void BtnScanCache_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanningCache || _isCacheBusy) return;
            await RefreshCacheAsync(preserveBanner: false);
        }

        private void BtnOpenCacheFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not CacheFolderInfo info) return;

            if (string.IsNullOrWhiteSpace(info.FullPath) || !Directory.Exists(info.FullPath))
            {
                ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus,
                    $"Folder '{info.Name}' tidak ditemukan.", BannerTone.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{info.FullPath}\"")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus,
                    $"Gagal membuka Explorer: {ex.Message}", BannerTone.Error);
            }
        }

        private async void BtnClearCacheFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_isCacheBusy || sender is not Button btn || btn.Tag is not CacheFolderInfo info)
                return;

            var confirm = MessageBox.Show(
                $"Bersihkan seluruh isi folder cache '{info.Name}' ({info.FormattedSize}, {info.FileCount} file)?\n\nLokasi: {info.FullPath}",
                "Konfirmasi Bersihkan Cache",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            _isCacheBusy = true;
            UpdateCacheButtonsEnabled();
            ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus,
                $"Membersihkan folder {info.Name}...", BannerTone.Info);

            try
            {
                var (ok, _, message) = await FiveMCacheHelper.ClearCacheFolderAsync(info.FullPath);
                ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus, message,
                    ok ? BannerTone.Success : BannerTone.Error);

                await RefreshCacheAsync(preserveBanner: true);
            }
            finally
            {
                _isCacheBusy = false;
                UpdateCacheButtonsEnabled();
            }
        }

        private async void BtnInstallCache_Click(object sender, RoutedEventArgs e)
        {
            if (_isCacheBusy) return;

            var dlg = new OpenFileDialog
            {
                Title = "Pilih Paket Cache FiveM (.zip atau .exe)",
                Filter = "Paket Cache (*.zip;*.exe)|*.zip;*.exe|ZIP Archive (*.zip)|*.zip|Installer (*.exe)|*.exe"
            };
            if (dlg.ShowDialog() != true) return;

            _isCacheBusy = true;
            UpdateCacheButtonsEnabled();

            bool installDone = false;
            var progress = new Progress<string>(msg =>
            {
                if (!installDone)
                    ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus, msg, BannerTone.Info);
            });

            try
            {
                var (ok, message) = await FiveMCacheHelper.InstallCachePackageAsync(
                    dlg.FileName, AppState.FiveMPath, progress);
                installDone = true;

                ShowBanner(CacheBanner, IconCacheBanner, TxtCacheStatus, message,
                    ok ? BannerTone.Success : BannerTone.Error);

                if (ok && Path.GetExtension(dlg.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                    await RefreshCacheAsync(preserveBanner: true);
            }
            finally
            {
                installDone = true;
                _isCacheBusy = false;
                UpdateCacheButtonsEnabled();
            }
        }

        // ── Shared Banner Helper ───────────────────────────────────────────

        private static void ShowBanner(Border banner, IconBlock icon, TextBlock textBlock, string message, BannerTone tone)
        {
            banner.Visibility = Visibility.Visible;
            textBlock.Text = message;

            switch (tone)
            {
                case BannerTone.Success:
                    banner.Background = new SolidColorBrush(Color.FromArgb(0x1E, 0x23, 0x86, 0x36));
                    banner.BorderBrush = new SolidColorBrush(Color.FromRgb(0x23, 0x86, 0x36));
                    icon.Icon = IconChar.CircleCheck;
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(0x3F, 0xB9, 0x50));
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC));
                    break;

                case BannerTone.Error:
                    banner.Background = new SolidColorBrush(Color.FromArgb(0x1E, 0xDA, 0x36, 0x33));
                    banner.BorderBrush = new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));
                    icon.Icon = IconChar.TriangleExclamation;
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC));
                    break;

                default:
                    banner.Background = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0x7A, 0x00));
                    banner.BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0x7A, 0x00));
                    icon.Icon = IconChar.CircleInfo;
                    icon.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00));
                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC));
                    break;
            }
        }
    }
}
