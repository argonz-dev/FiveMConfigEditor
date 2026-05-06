using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Views
{
    public partial class ConfigView : UserControl
    {
        private readonly MainWindow _main;

        public ConfigView(MainWindow main)
        {
            _main = main;
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            var d = AppState.Data ?? new IniData();
            TxtFiveMPath.Text = AppState.FiveMPath;
            TxtReShade5.Text = d.ReShade5;
            
            // Set UpdateChannel ComboBox
            foreach (ComboBoxItem item in CmbUpdateChannel.Items)
            {
                if (item.Tag.ToString() == d.UpdateChannel)
                {
                    CmbUpdateChannel.SelectedItem = item;
                    break;
                }
            }
            
            // Default to production if not set
            if (CmbUpdateChannel.SelectedItem == null)
                CmbUpdateChannel.SelectedIndex = 0;
        }

        private void BtnBrowseFiveM_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Pilih folder FiveM (yang berisi subfolder mods dan plugins)",
                UseDescriptionForTitle = true,
                SelectedPath = AppState.FiveMPath
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtFiveMPath.Text = dlg.SelectedPath;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate FiveM Path
            var fivemPath = TxtFiveMPath.Text.Trim();
            if (string.IsNullOrEmpty(fivemPath) || !System.IO.Directory.Exists(fivemPath))
            {
                MessageBox.Show("Folder FiveM tidak valid. Pastikan folder tersebut ada.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Save FiveM Path
            AppState.FiveMPath = fivemPath;
            AppState.SaveSettings();

            // Auto-detect and load CitizenFX.ini
            var iniPath = System.IO.Path.Combine(fivemPath, "CitizenFX.ini");
            if (System.IO.File.Exists(iniPath))
            {
                AppState.IniPath = iniPath;
                AppState.SaveSettings();
                
                // Load the ini file
                try
                {
                    AppState.Data = IniHelper.Load(iniPath);
                    
                    // Update with current values
                    AppState.Data.ReShade5 = TxtReShade5.Text.Trim();
                    var selectedItem = CmbUpdateChannel.SelectedItem as ComboBoxItem;
                    if (selectedItem != null)
                    {
                        AppState.Data.UpdateChannel = selectedItem.Tag.ToString() ?? "production";
                    }
                    
                    // Save back
                    IniHelper.Save(iniPath, AppState.Data);
                    
                    MessageBox.Show($"Konfigurasi disimpan.\n\nCitizenFX.ini terdeteksi dan dimuat:\n{iniPath}", "Sukses",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh main window to update watcher
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    mainWindow?.SetIniPath(iniPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Konfigurasi disimpan, tapi gagal load CitizenFX.ini:\n{ex.Message}", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                // Save INI data if file is selected
                if (!string.IsNullOrEmpty(AppState.IniPath))
                {
                    AppState.Data.ReShade5 = TxtReShade5.Text.Trim();
                    
                    // Save UpdateChannel
                    var selectedItem = CmbUpdateChannel.SelectedItem as ComboBoxItem;
                    if (selectedItem != null)
                    {
                        AppState.Data.UpdateChannel = selectedItem.Tag.ToString() ?? "production";
                    }
                    
                    IniHelper.Save(AppState.IniPath, AppState.Data);
                }
                
                MessageBox.Show("Konfigurasi global disimpan.\n\nCitizenFX.ini tidak ditemukan di folder FiveM.\nAnda dapat memilih file .ini secara manual di halaman Home.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
