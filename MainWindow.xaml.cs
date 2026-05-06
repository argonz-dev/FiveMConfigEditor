using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
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
        private GraphicsView? _graphicsView;
        private AiAssistantView? _aiAssistantView;
        private bool _isCheckingUpdate = false;
        private bool _hasUpdateAvailable = false;
        private bool _isManualUpdateCheck = false;

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
            _graphicsView   = new GraphicsView(this);
            _aiAssistantView = new AiAssistantView(this);

            // Auto-load ini dari settings sebelumnya
            if (!string.IsNullOrEmpty(AppState.IniPath) && File.Exists(AppState.IniPath))
            {
                AppState.Data = IniHelper.Load(AppState.IniPath);
                StartWatcher(AppState.IniPath);
            }

            NavigateTo("Home");

            // Setup AutoUpdater event handlers
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;

            // Check for updates after window is loaded (silent check)
            Loaded += (s, e) => CheckForUpdates(silent: true);
        }

        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            _isCheckingUpdate = false;
            UpdateUpdateButtonState();

            if (args.Error == null)
            {
                if (args.IsUpdateAvailable)
                {
                    _hasUpdateAvailable = true;
                    UpdateUpdateButtonState();

                    // Only show dialog if manually triggered
                    if (_isManualUpdateCheck)
                    {
                        var result = MessageBox.Show(
                            $"Update tersedia!\n\n" +
                            $"Versi saat ini: {args.CurrentVersion}\n" +
                            $"Versi baru: {args.InstalledVersion}\n\n" +
                            $"Apakah Anda ingin update sekarang?",
                            "Update Tersedia",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                if (AutoUpdater.DownloadUpdate(args))
                                {
                                    Application.Current.Shutdown();
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error saat download update:\n{ex.Message}", 
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
                else
                {
                    _hasUpdateAvailable = false;
                    UpdateUpdateButtonState();

                    // Only show "up to date" message if manually triggered
                    if (_isManualUpdateCheck)
                    {
                        MessageBox.Show(
                            $"Aplikasi sudah up to date!\n\nVersi saat ini: {args.CurrentVersion}",
                            "Up to Date",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                // Reset state on error
                _hasUpdateAvailable = false;
                UpdateUpdateButtonState();

                // Show friendly message if manually triggered, otherwise silent
                if (_isManualUpdateCheck)
                {
                    MessageBox.Show(
                        "Tidak dapat memeriksa update saat ini.\nSilakan coba lagi nanti.",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                
                // Always log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Update check failed: {args.Error?.Message}");
            }
        }

        private void CheckForUpdates(bool silent = false)
        {
            if (_isCheckingUpdate) return;

            _isCheckingUpdate = true;
            _isManualUpdateCheck = !silent;
            UpdateUpdateButtonState();

            try
            {
                // Configure AutoUpdater
                AutoUpdater.Mandatory = false;
                AutoUpdater.UpdateMode = Mode.Normal;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.ReportErrors = false; // Always false, we handle errors manually
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.DownloadPath = Path.Combine(Path.GetTempPath(), "FiveMConfigEditor");
                
                // GitHub raw URL for update.xml
                string updateUrl = "https://raw.githubusercontent.com/argonz-dev/FiveMConfigEditor/main/update.xml";
                
                AutoUpdater.Start(updateUrl);
            }
            catch (Exception ex)
            {
                _isCheckingUpdate = false;
                UpdateUpdateButtonState();

                if (!silent)
                {
                    MessageBox.Show($"Error saat cek update:\n{ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        private void UpdateUpdateButtonState()
        {
            Dispatcher.Invoke(() =>
            {
                if (_isCheckingUpdate)
                {
                    BtnCheckUpdate.Content = "⏳";
                    BtnCheckUpdate.ToolTip = "Checking for updates...";
                }
                else if (_hasUpdateAvailable)
                {
                    BtnCheckUpdate.Content = "🔄!";
                    BtnCheckUpdate.ToolTip = "Update available! Click to update";
                }
                else
                {
                    BtnCheckUpdate.Content = "🔄";
                    BtnCheckUpdate.ToolTip = "Check for Updates";
                }
            });
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
            CheckForUpdates(silent: false);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void BtnHome_Click(object sender, RoutedEventArgs e) => NavigateTo("Home");
        private void BtnConfig_Click(object sender, RoutedEventArgs e) => NavigateTo("Config");
        private void BtnHistory_Click(object sender, RoutedEventArgs e) => NavigateTo("History");
        private void BtnModManager_Click(object sender, RoutedEventArgs e) => NavigateTo("ModManager");
        private void BtnGraphics_Click(object sender, RoutedEventArgs e) => NavigateTo("Graphics");
        private void BtnAiAssistant_Click(object sender, RoutedEventArgs e) => NavigateTo("AiAssistant");

        public void NavigateTo(string page)
        {
            BtnHome.Style       = (Style)FindResource("SidebarButtonStyle");
            BtnConfig.Style     = (Style)FindResource("SidebarButtonStyle");
            BtnHistory.Style    = (Style)FindResource("SidebarButtonStyle");
            BtnModManager.Style = (Style)FindResource("SidebarButtonStyle");
            BtnGraphics.Style   = (Style)FindResource("SidebarButtonStyle");
            BtnAiAssistant.Style = (Style)FindResource("SidebarButtonStyle");

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
                case "Graphics":
                    BtnGraphics.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    _graphicsView?.Refresh();
                    MainContent.Content = _graphicsView;
                    break;
                case "AiAssistant":
                    BtnAiAssistant.Style = (Style)FindResource("SidebarButtonActiveStyle");
                    _aiAssistantView?.Refresh();
                    MainContent.Content = _aiAssistantView;
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
