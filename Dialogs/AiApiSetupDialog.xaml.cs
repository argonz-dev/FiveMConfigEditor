using System.Windows;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class AiApiSetupDialog : Window
    {
        public string ApiBaseUrl { get; private set; } = "";
        public string ApiKey { get; private set; } = "";

        public AiApiSetupDialog()
        {
            InitializeComponent();
            
            // Load existing settings if available
            TxtBaseUrl.Text = AppState.AiApiBaseUrl;
            TxtApiKey.Text = AppState.AiApiKey;
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string baseUrl = TxtBaseUrl.Text.Trim();
            string apiKey = TxtApiKey.Text.Trim();

            if (string.IsNullOrEmpty(baseUrl))
            {
                MessageBox.Show("Please enter API Base URL", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter API Key", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ApiBaseUrl = baseUrl;
            ApiKey = apiKey;
            DialogResult = true;
            Close();
        }
    }
}
