using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using FiveMConfigEditorWPF.Models;
using FiveMConfigEditorWPF.Views;
using AutoUpdaterDotNET;

namespace FiveMConfigEditorWPF
{
    public partial class MainWindow : Window
    {
        private FileSystemWatcher? _watcher;
        private HomeView? _homeView;
        private ConfigView? _configView;
        private HistoryView? _historyView;
        private ModManagerView? _modManagerView;

        public MainWindow()
        {
            InitializeComponent();
            AppState.LoadSettings();

            // First run setup - check if FiveM path is configured
            if (string.IsNullOrEmpty(AppState.FiveMPath) || !Directory.Exists(AppState.FiveMPath))
            {
                var setupDialog = new Dialogs.FirstRunSetupDialog();
                if (setupDialog.ShowDialog() != true)
                {
                    // User cancelled setup, exit application
                    Application.Current.Shutdown();
                    return;
                }

                // Save FiveM path from setup
                AppState.FiveMPath = setupDialog.SelectedFiveMPath;
                AppState.SaveSettings();

                // Auto-detect CitizenFX.ini
                var iniPath = Path.Combine(AppState.FiveMPath, "CitizenFX.ini");
                if (File.Exists(iniPath))
                {
                    AppState.IniPath = iniPath;
                    AppState.SaveSettings();
                }
            }

            AppState.Presets   = PresetManager.Load();
            AppState.Snapshots = SnapshotManager.Load();

            _homeView       = new HomeView(this);
            _configView     = new ConfigView(this);
            _historyView    = new HistoryView(this);
            _modManagerView = new ModManagerView(this);

            // Auto-load ini dari settings sebelumnya
            if (!string.IsNullOrEmpty(AppState.IniPath) && File.Exists(AppState.IniPath))
            {
                AppState.Data = IniHelper.Load(AppState.IniPath);
                StartWatcher(AppState.IniPath);
            }

            NavigateTo("Home");

            // Check for updates after window is loaded
            Loaded += (s, e) => CheckForUpdates();
        }

        private void CheckForUpdates()
        {
            try
            {
                // Configure AutoUpdater
                AutoUpdater.Mandatory = false;
                AutoUpdater.UpdateMode = Mode.ForcedDownload;
                AutoUpdater.ShowSkipButton = true;
                AutoUpdater.ShowRemindLaterButton = true;
                AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Days;
                AutoUpdater.RemindLaterAt = 1;
                
                // Set custom colors to match app theme
                AutoUpdater.BasicAuthChangeLog = null;
                
                // GitHub raw URL for update.xml
                // Format: https://raw.githubusercontent.com/USERNAME/REPO/main/update.xml
                string updateUrl = "https://raw.githubusercontent.com/YOUR_USERNAME/FiveMConfigEditor/main/update.xml";
                
                AutoUpdater.Start(updateUrl);
            }
            catch (Exception ex)
            {
                // Silent fail - don't interrupt user experience
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void BtnHome_Click(object sender, RoutedEventArgs e) => NavigateTo("Home");
        private void BtnConfig_Click(object sender, RoutedEventArgs e) => NavigateTo("Config");
        private void BtnHistory_Click(object sender, RoutedEventArgs e) => NavigateTo("History");
        private void BtnModManager_Click(object sender, RoutedEventArgs e) => NavigateTo("ModManager");

        public void NavigateTo(string page)
        {
            BtnHome.Style       = (Style)FindResource("SidebarButtonStyle");
            BtnConfig.Style     = (Style)FindResource("SidebarButtonStyle");
            BtnHistory.Style    = (Style)FindResource("SidebarButtonStyle");
            BtnModManager.Style = (Style)FindResource("SidebarButtonStyle");

            switch (page)
            {
                case "Home":
                    BtnHome.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    _homeView?.Refresh();
                    MainContent.Content = _homeView;
                    break;
                case "Config":
                    BtnConfig.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    _configView?.Refresh();
                    MainContent.Content = _configView;
                    break;
                case "History":
                    BtnHistory.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    _historyView?.Refresh();
                    MainContent.Content = _historyView;
                    break;
                case "ModManager":
                    BtnModManager.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    _modManagerView?.Refresh();
                    MainContent.Content = _modManagerView;
                    break;
            }
        }

        public void SetIniPath(string path)
        {
            AppState.IniPath = path;
            AppState.SaveSettings(); // Simpan ke settings.json
            if (File.Exists(path))
            {
                AppState.Data = IniHelper.Load(path);
                StartWatcher(path);
            }
        }

        private void StartWatcher(string path)
        {
            _watcher?.Dispose();
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path);
            if (dir == null || file == null) return;

            _watcher = new FileSystemWatcher(dir, file)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFileChanged;
            AppState.WatcherActive = true;
            UpdateLed(true);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    System.Threading.Thread.Sleep(200);
                    AppState.Data = IniHelper.Load(AppState.IniPath);
                    var snap = new Snapshot
                    {
                        Label = "Auto-detected change",
                        SourceFile = AppState.IniPath,
                        Data = AppState.Data,
                        CapturedAt = DateTime.Now
                    };
                    SnapshotManager.Add(AppState.Snapshots, snap);
                    _configView?.Refresh();
                    _historyView?.Refresh(); // Refresh status view to detect changes
                }
                catch { }
            });
        }

        public void UpdateLed(bool active)
        {
            LedStatus.Fill = active
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            TxtLedLabel.Text = active ? "ON" : "OFF";
            TxtLedLabel.Foreground = active
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        }
    }
}
