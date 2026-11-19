using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Dialogs
{
    public partial class ReleaseNotesDialog : Window
    {
        private readonly string _gitHubUrl;

        public ReleaseNotesDialog(string currentVersion, string newVersion, ReleaseNotesInfo? notes)
        {
            InitializeComponent();

            var cur = NormalizeVersion(currentVersion);
            var next = NormalizeVersion(newVersion);
            TxtVersionBadge.Text = $"v{cur} → v{next}";

            var title = notes != null && !string.IsNullOrWhiteSpace(notes.Name)
                ? notes.Name.Trim()
                : $"Pembaruan v{next}";
            TxtReleaseTitle.Text = title;

            if (notes != null && !string.IsNullOrWhiteSpace(notes.PublishedAt))
            {
                if (DateTimeOffset.TryParse(notes.PublishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
                {
                    TxtPublishedDate.Text = $"Dirilis: {dto.ToLocalTime():dd MMM yyyy HH:mm}";
                }
                else
                {
                    TxtPublishedDate.Text = $"Dirilis: {notes.PublishedAt}";
                }
            }
            else
            {
                TxtPublishedDate.Visibility = Visibility.Collapsed;
            }

            if (notes != null && notes.Ok && !string.IsNullOrWhiteSpace(notes.Body))
            {
                TxtReleaseBody.Text = notes.Body.Trim();
            }
            else if (notes != null && !string.IsNullOrWhiteSpace(notes.Error))
            {
                TxtReleaseBody.Text = $"Catatan rilis tidak dapat dimuat dari GitHub ({notes.Error}).\n\nAnda tetap dapat melanjutkan pembaruan ke v{next}.";
            }
            else
            {
                TxtReleaseBody.Text = $"Catatan rilis tidak tersedia untuk versi v{next}.\n\nKlik 'Update Sekarang' untuk mengunduh dan memasang pembaruan.";
            }

            _gitHubUrl = notes?.HtmlUrl?.Trim() ?? "";
            var hasValidGitHubUrl = _gitHubUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase);
            BtnOpenGitHub.Visibility = hasValidGitHubUrl ? Visibility.Visible : Visibility.Collapsed;
            BtnOpenGitHub.IsEnabled = hasValidGitHubUrl;
        }

        private static string NormalizeVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.0.0";
            return version.Trim().TrimStart('v', 'V');
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnOpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            if (!_gitHubUrl.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                Process.Start(new ProcessStartInfo(_gitHubUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membuka browser: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLater_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnUpdateNow_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
