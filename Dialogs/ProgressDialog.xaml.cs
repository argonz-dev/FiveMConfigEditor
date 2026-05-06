using System.Windows;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class ProgressDialog : Window
    {
        public ProgressDialog(string title, string status)
        {
            InitializeComponent();
            TxtTitle.Text = title;
            TxtStatus.Text = status;
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() => TxtStatus.Text = status);
        }

        public void SetIcon(string icon)
        {
            Dispatcher.Invoke(() => TxtIcon.Text = icon);
        }
    }
}
