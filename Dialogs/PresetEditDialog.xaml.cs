using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class PresetEditDialog : Window
    {
        private readonly Preset _preset;

        public PresetEditDialog(Preset preset)
        {
            _preset = preset;
            InitializeComponent();
            TxtName.Text = preset.Name;
            TxtDesc.Text = preset.Description;
            TxtServerUrl.Text = preset.ServerUrl;
            TxtKeptNickname.Text = preset.KeptNickname ?? "";
            TxtImagePath.Text = preset.ImagePath;
            TxtBannerPath.Text = preset.BannerPath ?? "";
            UpdateImagePreview(preset.ImagePath);
            UpdateBannerPreview(preset.BannerPath ?? "");
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.bmp;*.ico)|*.png;*.jpg;*.bmp;*.ico|All Files (*.*)|*.*",
                Title = "Pilih gambar preset"
            };
            if (dlg.ShowDialog() != true) return;

            // Convert .ico ke .png otomatis
            var finalPath = ImageHelper.EnsurePng(dlg.FileName);
            TxtImagePath.Text = finalPath;
            UpdateImagePreview(finalPath);
        }

        private void UpdateImagePreview(string path)
        {
            ImgPreview.Source = ImageHelper.LoadImage(path);
        }

        private void UpdateBannerPreview(string path)
        {
            ImgBannerPreview.Source = ImageHelper.LoadImage(path);
        }

        private async void BtnFetchServerAssets_Click(object sender, RoutedEventArgs e)
        {
            string serverUrl = TxtServerUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                MessageBox.Show("Isi Server Connect / Host terlebih dahulu.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BtnFetchServerAssets.IsEnabled = false;
            BtnFetchServerAssets.Content = "⏳ Mengambil...";
            try
            {
                var (ok, iconPath, bannerPath, err) = await ServerProbeHelper.FetchAndSaveServerAssetsAsync(serverUrl, _preset.Id);
                if (!ok)
                {
                    MessageBox.Show($"Gagal mengambil logo/banner server:\n{err}", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(iconPath))
                {
                    TxtImagePath.Text = iconPath;
                    UpdateImagePreview(iconPath);
                }
                if (!string.IsNullOrEmpty(bannerPath))
                {
                    TxtBannerPath.Text = bannerPath;
                    UpdateBannerPreview(bannerPath);
                }
            }
            finally
            {
                BtnFetchServerAssets.IsEnabled = true;
                BtnFetchServerAssets.Content = "🌐 Ambil Logo & Banner";
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Nama preset tidak boleh kosong.", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _preset.Name = TxtName.Text.Trim();
            _preset.Description = TxtDesc.Text.Trim();
            _preset.ServerUrl = TxtServerUrl.Text.Trim();
            _preset.KeptNickname = TxtKeptNickname.Text.Trim();
            _preset.ImagePath = TxtImagePath.Text.Trim();
            _preset.BannerPath = TxtBannerPath.Text.Trim();
            _preset.UpdatedAt = DateTime.Now;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
