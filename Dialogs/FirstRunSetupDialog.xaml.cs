using System;
using System.IO;
using System.Windows;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class FirstRunSetupDialog : Window
    {
        public string SelectedFiveMPath { get; private set; } = "";
        private bool _isValid = false;

        public FirstRunSetupDialog()
        {
            InitializeComponent();
            
            // Try to detect default FiveM path
            var defaultPath = @"d:\FiveM\FiveM.app";
            if (Directory.Exists(defaultPath))
            {
                TxtFiveMPath.Text = defaultPath;
                ValidatePath(defaultPath);
            }
        }

        private void BtnBrowseFiveM_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Pilih folder FiveM (yang berisi subfolder mods, plugins, dan CitizenFX.ini)",
                UseDescriptionForTitle = true,
                SelectedPath = TxtFiveMPath.Text
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtFiveMPath.Text = dlg.SelectedPath;
                ValidatePath(dlg.SelectedPath);
            }
        }

        private void ValidatePath(string path)
        {
            PanelDetection.Visibility = Visibility.Collapsed;
            PanelError.Visibility = Visibility.Collapsed;
            _isValid = false;
            BtnContinue.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                TxtError.Text = "Folder tidak ditemukan atau tidak valid";
                PanelError.Visibility = Visibility.Visible;
                TxtValidation.Text = "❌ Folder tidak valid";
                return;
            }

            // Check for mods and plugins folders
            var modsPath = Path.Combine(path, "mods");
            var pluginsPath = Path.Combine(path, "plugins");
            var iniPath = Path.Combine(path, "CitizenFX.ini");

            // Count items
            int modsCount = 0;
            int pluginsCount = 0;
            bool hasIni = File.Exists(iniPath);

            try
            {
                if (Directory.Exists(modsPath))
                {
                    modsCount = Directory.GetFiles(modsPath, "*.rpf").Length + 
                                Directory.GetDirectories(modsPath).Length;
                }

                if (Directory.Exists(pluginsPath))
                {
                    pluginsCount = Directory.GetFiles(pluginsPath, "*.asi").Length +
                                   Directory.GetFiles(pluginsPath, "*.dll").Length +
                                   Directory.GetDirectories(pluginsPath).Length;
                }
            }
            catch (Exception ex)
            {
                TxtError.Text = $"Error membaca folder: {ex.Message}";
                PanelError.Visibility = Visibility.Visible;
                TxtValidation.Text = "❌ Error membaca folder";
                return;
            }

            // Show detection results
            TxtModsCount.Text = $"{modsCount} item";
            TxtPluginsCount.Text = $"{pluginsCount} item";
            TxtIniStatus.Text = hasIni ? "✅ Ditemukan" : "❌ Tidak ditemukan";
            TxtIniStatus.Foreground = hasIni 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x4D, 0x4D));

            PanelDetection.Visibility = Visibility.Visible;
            _isValid = true;
            BtnContinue.IsEnabled = true;
            TxtValidation.Text = "✅ Folder valid - Klik Lanjutkan untuk memulai";
            SelectedFiveMPath = path;
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            if (!_isValid)
            {
                MessageBox.Show("Pilih folder FiveM yang valid terlebih dahulu.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Aplikasi memerlukan folder FiveM untuk berfungsi.\nKeluar dari aplikasi?",
                "Konfirmasi",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
