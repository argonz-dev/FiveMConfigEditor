using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using FiveMConfigEditorWPF.Models;
namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class PresetConfigDialog : Window
    {
        private readonly Preset _preset;
        private ObservableCollection<PoolEntry> _poolEntries = new();
        private bool _isManualTab = false;

        private static readonly SolidColorBrush ActiveBg   = new(Color.FromRgb(0x00, 0x78, 0xD4));
        private static readonly SolidColorBrush InactiveBg = new(Color.FromRgb(0x1E, 0x1E, 0x32));
        private static readonly SolidColorBrush ActiveFg   = new(Colors.White);
        private static readonly SolidColorBrush InactiveFg = new(Color.FromRgb(0x88, 0x88, 0x88));

        public PresetConfigDialog(Preset preset)
        {
            _preset = preset;
            InitializeComponent();
            TxtTitle.Text = $"Config — {preset.Name}";
            GridPools.ItemsSource = _poolEntries;
            LoadFromPreset();
        }

        private void LoadFromPreset()
        {
            var d = _preset.Data;
            TxtIVPath.Text          = d.IVPath;
            TxtBuildNumber.Text     = d.SavedBuildNumber;
            ChkReplaceExe.IsChecked = d.ReplaceExecutable == "1";
            TxtReShade5.Text        = d.ReShade5;
            _poolEntries.Clear();
            foreach (var kv in d.PoolSizes)
                _poolEntries.Add(new PoolEntry { Key = kv.Key, Value = kv.Value });
            TxtRawIni.Text = BuildRawIni(d);
        }

        private string BuildRawIni(IniData d)
        {
            string poolJson = d.PoolSizes.Count == 0
                ? "{}"
                : JsonSerializer.Serialize(d.PoolSizes);
            var sb = new StringBuilder();
            sb.AppendLine("[Game]");
            sb.AppendLine($"IVPath={d.IVPath}");
            sb.AppendLine($"SavedBuildNumber={d.SavedBuildNumber}");
            sb.AppendLine($"PoolSizesIncrease={poolJson}");
            sb.AppendLine($"ReplaceExecutable={d.ReplaceExecutable}");
            sb.AppendLine();
            sb.AppendLine("[Addons]");
            sb.AppendLine($"    ReShade5={d.ReShade5}");
            return sb.ToString();
        }

        // ── Tab switching ─────────────────────────────────────────────────────
        private void BtnTabForm_Click(object sender, RoutedEventArgs e)
        {
            _isManualTab = false;
            PanelForm.Visibility   = Visibility.Visible;
            PanelManual.Visibility = Visibility.Collapsed;
            BtnTabForm.Background   = ActiveBg;
            BtnTabForm.Foreground   = ActiveFg;
            BtnTabManual.Background = InactiveBg;
            BtnTabManual.Foreground = InactiveFg;
        }

        private void BtnTabManual_Click(object sender, RoutedEventArgs e)
        {
            // Sync raw text dari form sebelum pindah
            TxtRawIni.Text = BuildRawIni(CollectFormData());
            _isManualTab = true;
            PanelForm.Visibility   = Visibility.Collapsed;
            PanelManual.Visibility = Visibility.Visible;
            BtnTabManual.Background = ActiveBg;
            BtnTabManual.Foreground = ActiveFg;
            BtnTabForm.Background   = InactiveBg;
            BtnTabForm.Foreground   = InactiveFg;
        }

        // ── Manual tab ────────────────────────────────────────────────────────
        private void BtnParseRaw_Click(object sender, RoutedEventArgs e)
        {
            var parsed = IniHelper.ParseText(TxtRawIni.Text);
            TxtIVPath.Text          = parsed.IVPath;
            TxtBuildNumber.Text     = parsed.SavedBuildNumber;
            ChkReplaceExe.IsChecked = parsed.ReplaceExecutable == "1";
            TxtReShade5.Text        = parsed.ReShade5;
            _poolEntries.Clear();
            foreach (var kv in parsed.PoolSizes)
                _poolEntries.Add(new PoolEntry { Key = kv.Key, Value = kv.Value });
            // Switch ke Form
            BtnTabForm_Click(sender, e);
        }

        // ── Pool buttons ──────────────────────────────────────────────────────
        private void BtnAddPool_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddPoolDialog { Owner = this };
            if (dlg.ShowDialog() == true && !string.IsNullOrEmpty(dlg.PoolKey))
            {
                for (int i = _poolEntries.Count - 1; i >= 0; i--)
                    if (_poolEntries[i].Key == dlg.PoolKey) _poolEntries.RemoveAt(i);
                _poolEntries.Add(new PoolEntry { Key = dlg.PoolKey, Value = dlg.PoolValue });
            }
        }

        private void BtnRemovePool_Click(object sender, RoutedEventArgs e)
        {
            if (GridPools.SelectedItem is PoolEntry entry) _poolEntries.Remove(entry);
        }

        private void BtnClearPools_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Hapus semua pool sizes?", "Konfirmasi",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                _poolEntries.Clear();
        }

        private void BtnBrowseGTA_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Pilih folder GTA V",
                UseDescriptionForTitle = true
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtIVPath.Text = dlg.SelectedPath;
        }

        // ── Save / Cancel ─────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _preset.Data = _isManualTab
                ? IniHelper.ParseText(TxtRawIni.Text)
                : CollectFormData();
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void BtnClose_Click(object sender, RoutedEventArgs e)  => DialogResult = false;

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private IniData CollectFormData() => new()
        {
            IVPath            = TxtIVPath.Text.Trim(),
            SavedBuildNumber  = TxtBuildNumber.Text.Trim(),
            ReplaceExecutable = ChkReplaceExe.IsChecked == true ? "1" : "0",
            ReShade5          = TxtReShade5.Text.Trim(),
            PoolSizes         = CollectPools()
        };

        private Dictionary<string, int> CollectPools()
        {
            var d = new Dictionary<string, int>();
            foreach (var e in _poolEntries) d[e.Key] = e.Value;
            return d;
        }
    }
}
