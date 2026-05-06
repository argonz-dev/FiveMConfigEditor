using System;
using System.Linq;
using System.Windows;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class ImportPackConfirmDialog : Window
    {
        public bool CreateBackup => ChkCreateBackup.IsChecked == true;
        public string BackupName => TxtBackupName.Text.Trim();

        public ImportPackConfirmDialog(ModPackMetadata metadata)
        {
            InitializeComponent();

            // Set pack info
            TxtPackName.Text = metadata.Name;
            TxtPackAuthor.Text = metadata.Author;
            TxtPackDate.Text = metadata.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            
            var sizeDisplay = metadata.TotalSizeBytes < 1024 * 1024
                ? $"{metadata.TotalSizeBytes / 1024.0:F1} KB"
                : $"{metadata.TotalSizeBytes / (1024.0 * 1024):F1} MB";
            TxtPackSize.Text = sizeDisplay;
            
            TxtPackMods.Text = $"{metadata.ModFiles.Count} file/folder";
            TxtPackPlugins.Text = $"{metadata.PluginFiles.Count} file/folder";

            // Get current state
            try
            {
                var currentMods = ModManager.ScanMods();
                var currentPlugins = ModManager.ScanPlugins();
                
                int activeMods = currentMods.Count(m => m.IsEnabled);
                int activePlugins = currentPlugins.Count(p => p.IsEnabled);
                
                TxtCurrentMods.Text = $"{activeMods} item aktif dari {currentMods.Count} total";
                TxtCurrentPlugins.Text = $"{activePlugins} item aktif dari {currentPlugins.Count} total";

                // Auto-generate backup name with timestamp
                TxtBackupName.Text = $"Backup sebelum import - {DateTime.Now:dd/MM/yyyy HH:mm}";

                // If no active mods/plugins, suggest not creating backup
                if (activeMods == 0 && activePlugins == 0)
                {
                    ChkCreateBackup.IsChecked = false;
                }
            }
            catch
            {
                TxtCurrentMods.Text = "Error membaca";
                TxtCurrentPlugins.Text = "Error membaca";
            }

            ChkCreateBackup.Checked += (s, e) => PanelBackupName.Visibility = Visibility.Visible;
            ChkCreateBackup.Unchecked += (s, e) => PanelBackupName.Visibility = Visibility.Collapsed;
            PanelBackupName.Visibility = ChkCreateBackup.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (CreateBackup && string.IsNullOrWhiteSpace(BackupName))
            {
                MessageBox.Show("Masukkan nama untuk backup preset.", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
