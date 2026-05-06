using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FiveMConfigEditorWPF.Models;

namespace FiveMConfigEditorWPF.Views
{
    public partial class AiAssistantView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly AiChatService _aiService;
        private bool _isProcessing = false;

        public AiAssistantView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            // Initialize AI service with API credentials from settings
            // If API key is empty, show setup dialog
            if (string.IsNullOrEmpty(AppState.AiApiKey))
            {
                ShowApiKeySetupPrompt();
            }
            
            // Initialize service (will use default or configured values)
            _aiService = new AiChatService(AppState.AiApiBaseUrl, AppState.AiApiKey);
        }

        private void ShowApiKeySetupPrompt()
        {
            var result = MessageBox.Show(
                "AI Assistant requires API configuration.\n\n" +
                "Would you like to configure it now?",
                "AI Setup Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                var dialog = new Dialogs.AiApiSetupDialog();
                if (dialog.ShowDialog() == true)
                {
                    AppState.AiApiBaseUrl = dialog.ApiBaseUrl;
                    AppState.AiApiKey = dialog.ApiKey;
                    AppState.SaveSettings();
                }
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void TxtUserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async System.Threading.Tasks.Task SendMessageAsync()
        {
            if (_isProcessing) return;

            string userMessage = TxtUserInput.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            _isProcessing = true;
            BtnSend.IsEnabled = false;
            TxtUserInput.IsEnabled = false;
            BtnSend.Content = "...";

            // Add user message to UI
            AddMessageToChat(userMessage, isUser: true);
            TxtUserInput.Clear();

            try
            {
                // Send to AI and get response
                string response = await _aiService.SendMessageAsync(userMessage);
                
                // Add AI response to UI
                AddMessageToChat(response, isUser: false);
            }
            catch (Exception ex)
            {
                AddMessageToChat($"Error: {ex.Message}", isUser: false, isError: true);
            }
            finally
            {
                _isProcessing = false;
                BtnSend.IsEnabled = true;
                TxtUserInput.IsEnabled = true;
                BtnSend.Content = "Send";
                TxtUserInput.Focus();
            }
        }

        private void AddMessageToChat(string message, bool isUser, bool isError = false)
        {
            var border = new Border
            {
                Background = isUser 
                    ? new SolidColorBrush(Color.FromRgb(0x3A, 0x25, 0x10))
                    : isError
                        ? new SolidColorBrush(Color.FromRgb(0x4A, 0x20, 0x20))
                        : new SolidColorBrush(Color.FromRgb(0x2A, 0x1F, 0x14)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 600
            };

            var stackPanel = new StackPanel();

            // Header with icon and timestamp
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var icon = new TextBlock
            {
                Text = isUser ? "👤" : isError ? "⚠️" : "🤖",
                FontSize = 12,
                Margin = new Thickness(0, 0, 6, 0)
            };

            var nameText = new TextBlock
            {
                Text = isUser ? "You" : isError ? "Error" : "AI Assistant",
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00)),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontSize = 10
            };

            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(nameText);
            headerPanel.Children.Add(timeText);

            // Message content
            var messageText = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0x88, 0x66)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(headerPanel);
            stackPanel.Children.Add(messageText);
            border.Child = stackPanel;

            ChatMessagesPanel.Children.Add(border);
            ChatScrollViewer.ScrollToBottom();
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the chat history?",
                "Clear Chat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear all messages
                ChatMessagesPanel.Children.Clear();

                // Clear service history
                _aiService.ClearHistory();
            }
        }

        private async void BtnQuickOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            TxtUserInput.Text = "What are the best optimization tips for GTA V on FiveM?";
            await SendMessageAsync();
        }

        private async void BtnQuickRTX_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            TxtUserInput.Text = "What are the recommended settings for RTX 3060 Ti at 1080p targeting 60 FPS?";
            await SendMessageAsync();
        }

        private async void BtnQuickLowFPS_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            TxtUserInput.Text = "I'm experiencing low FPS in FiveM. What settings should I change to improve performance?";
            await SendMessageAsync();
        }

        public void Refresh()
        {
            // Refresh if needed
        }
    }
}
