using System;
using System.Windows;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class ExportModPackDialog : Window
    {
        public string PackName { get; private set; } = "";
        public string PackDescription { get; private set; } = "";
        public string PackAuthor { get; private set; } = "";

        public ExportModPackDialog()
        {
            InitializeComponent();
            // Auto-generate nama dan author
            PackName = $"Graphics Pack {DateTime.Now:dd-MM-yyyy HHmm}";
            PackAuthor = Environment.UserName;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            PackDescription = TxtDesc.Text.Trim();
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
