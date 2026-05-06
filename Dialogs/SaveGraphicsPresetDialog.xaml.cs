using System.Windows;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class SaveGraphicsPresetDialog : Window
    {
        public string PresetName        { get; private set; } = "";
        public string PresetDescription { get; private set; } = "";

        public SaveGraphicsPresetDialog() => InitializeComponent();

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Nama preset tidak boleh kosong.", "Validasi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            PresetName        = TxtName.Text.Trim();
            PresetDescription = TxtDesc.Text.Trim();
            DialogResult      = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
