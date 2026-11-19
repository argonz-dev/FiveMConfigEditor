using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class AiApiSetupDialog : Window
    {
        public string ApiBaseUrl { get; private set; } = "";
        public string ApiKey { get; private set; } = "";
        public string Model { get; private set; } = "";

        public AiApiSetupDialog()
        {
            InitializeComponent();
            
            TxtBaseUrl.Text = string.IsNullOrEmpty(AppState.AiApiBaseUrl) ? SecureConfig.GetApiBaseUrl() : AppState.AiApiBaseUrl;
            TxtApiKey.Text = string.IsNullOrEmpty(AppState.AiApiKey) ? SecureConfig.GetApiKey() : AppState.AiApiKey;
            TxtModel.Text = string.IsNullOrEmpty(AppState.AiModel) ? SecureConfig.GetDefaultModel() : AppState.AiModel;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnPresetVpsIp_Click(object sender, RoutedEventArgs e)
        {
            TxtBaseUrl.Text = "https://103.103.22.86:20128/v1";
            TxtApiKey.Text = "tuxevil";
            TxtModel.Text = "gemini-3.8-flash-high";
            TxtTestStatus.Text = "Preset VPS 103 (IP) dipilih.";
            TxtTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
        }

        private void BtnPresetVpsDomain_Click(object sender, RoutedEventArgs e)
        {
            TxtBaseUrl.Text = "https://argonzbuilder.my.id:20128/v1";
            TxtApiKey.Text = "tuxevil";
            TxtModel.Text = "gemini-3.8-flash-high";
            TxtTestStatus.Text = "Preset VPS 103 (Domain) dipilih.";
            TxtTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
        }

        private void BtnPresetLocalhost_Click(object sender, RoutedEventArgs e)
        {
            TxtBaseUrl.Text = "http://127.0.0.1:20128/v1";
            TxtApiKey.Text = "tuxevil";
            TxtModel.Text = "argzgemini";
            TxtTestStatus.Text = "Preset Localhost dipilih.";
            TxtTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
        }

        private void CboModelPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboModelPresets.SelectedItem is ComboBoxItem item)
            {
                string text = item.Content.ToString() ?? "";
                if (!text.StartsWith("--"))
                {
                    TxtModel.Text = text;
                }
            }
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            BtnTest.IsEnabled = false;
            TxtTestStatus.Text = "Menguji koneksi...";
            TxtTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00));

            var result = await AiChatService.TestConnectionAsync(TxtBaseUrl.Text, TxtApiKey.Text, TxtModel.Text);
            
            BtnTest.IsEnabled = true;
            if (result.Success)
            {
                TxtTestStatus.Text = "✓ Sukses: Terhubung ke provider!";
                TxtTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43));
            }
            else
            {
                TxtTestStatus.Text = $"✕ Gagal: {result.Message}";
                TxtTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string baseUrl = TxtBaseUrl.Text.Trim();
            string apiKey = TxtApiKey.Text.Trim();
            string model = TxtModel.Text.Trim();

            if (string.IsNullOrEmpty(baseUrl))
            {
                MessageBox.Show("Silakan masukkan API Base URL.", "Validasi Input", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(model))
            {
                model = "gemini-3.8-flash-high";
            }

            ApiBaseUrl = baseUrl;
            ApiKey = apiKey;
            Model = model;
            DialogResult = true;
            Close();
        }
    }
}