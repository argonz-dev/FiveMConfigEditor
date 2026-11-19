using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private AiChatService _aiService;
        private bool _isProcessing = false;

        public AiAssistantView(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            // Inisialisasi konfigurasi default dari AppState / SecureConfig
            if (string.IsNullOrEmpty(AppState.AiApiBaseUrl))
                AppState.AiApiBaseUrl = SecureConfig.GetApiBaseUrl();
            if (string.IsNullOrEmpty(AppState.AiApiKey))
                AppState.AiApiKey = SecureConfig.GetApiKey();
            if (string.IsNullOrEmpty(AppState.AiModel))
                AppState.AiModel = SecureConfig.GetDefaultModel();
            
            _aiService = new AiChatService(AppState.AiApiBaseUrl, AppState.AiApiKey, AppState.AiModel);
            UpdateStatusDisplay();

            // Tambahkan pesan sambutan pertama kali
            AddWelcomeMessage();
        }

        private void UpdateStatusDisplay()
        {
            string host = "VPS 103";
            if (AppState.AiApiBaseUrl.Contains("127.0.0.1") || AppState.AiApiBaseUrl.Contains("localhost"))
                host = "Localhost";
            else if (!AppState.AiApiBaseUrl.Contains("103.") && !AppState.AiApiBaseUrl.Contains("argonzbuilder"))
                host = "Custom";

            TxtProviderStatus.Text = $"{host} ({AppState.AiModel})";
        }

        private class MessageSegment
        {
            public bool IsCodeOrAction { get; set; }
            public string Language { get; set; } = "";
            public string Content { get; set; } = "";
            public string ActionType { get; set; } = "";
            public string ActionParam { get; set; } = "";
        }

        private void AddWelcomeMessage()
        {
            AddMessageToChat("Halo! Saya AI Assistant resmi FiveM Config Editor.\n\n" +
                "Saya memiliki akses eksekusi langsung ke sistem lokal Anda: menjalankan perintah PowerShell, membersihkan cache FiveM, menerapkan tuning FPS ke gta5_settings.xml, dan mengoptimasi memory pools CitizenFX.ini secara otomatis.\n\n" +
                "Tuliskan apa yang ingin Anda optimalkan atau gunakan tombol Aksi Cepat di bawah!",
                isUser: false);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Dialogs.AiApiSetupDialog();
            if (dialog.ShowDialog() == true)
            {
                AppState.AiApiBaseUrl = dialog.ApiBaseUrl;
                AppState.AiApiKey = dialog.ApiKey;
                AppState.AiModel = dialog.Model;
                AppState.SaveSettings();

                _aiService = new AiChatService(AppState.AiApiBaseUrl, AppState.AiApiKey, AppState.AiModel);
                UpdateStatusDisplay();
                AddMessageToChat($"Konfigurasi provider diperbarui: {AppState.AiApiBaseUrl} [{AppState.AiModel}]", isUser: false);
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

        private async Task SendMessageAsync(string? overrideMessage = null, bool forceAutoRun = false)
        {
            if (_isProcessing) return;

            string userMessage = overrideMessage ?? TxtUserInput.Text.Trim();
            if (string.IsNullOrEmpty(userMessage)) return;

            _isProcessing = true;
            BtnSend.IsEnabled = false;
            TxtUserInput.IsEnabled = false;
            BtnSend.Content = "...";

            // Add user message to UI
            AddMessageToChat(userMessage, isUser: true);
            if (overrideMessage == null)
            {
                TxtUserInput.Clear();
            }

            string lower = userMessage.ToLowerInvariant();
            bool userRequestedExecution = forceAutoRun ||
                lower.Contains("jalankan") || lower.Contains("eksekusi") || lower.Contains("powershell") ||
                lower.Contains("run") || lower.Contains("bisa nggak") || lower.Contains("bisa gak") ||
                lower.Contains("bersihkan") || lower.Contains("optimasi sekarang") || lower.Contains("terapkan");

            try
            {
                // Send to AI and get response
                string response = await _aiService.SendMessageAsync(userMessage);

                // Add AI response to UI and trigger execution if requested
                AddMessageToChat(response, isUser: false, isError: false, autoExecute: userRequestedExecution);
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

                // Restore button content with icon
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(new IconBlock { Icon = IconChar.PaperPlane, FontSize = 11, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
                panel.Children.Add(new TextBlock { Text = "Kirim", VerticalAlignment = VerticalAlignment.Center });
                BtnSend.Content = panel;

                TxtUserInput.Focus();
            }
        }

        private List<MessageSegment> ParseMessageSegments(string raw)
        {
            var list = new List<MessageSegment>();
            if (string.IsNullOrEmpty(raw)) return list;

            int idx = 0;
            while (idx < raw.Length)
            {
                int startFence = raw.IndexOf("```", idx);
                if (startFence == -1)
                {
                    string remaining = raw.Substring(idx).Trim();
                    if (!string.IsNullOrEmpty(remaining))
                        list.Add(new MessageSegment { IsCodeOrAction = false, Content = remaining });
                    break;
                }

                if (startFence > idx)
                {
                    string textBefore = raw.Substring(idx, startFence - idx).Trim();
                    if (!string.IsNullOrEmpty(textBefore))
                        list.Add(new MessageSegment { IsCodeOrAction = false, Content = textBefore });
                }

                int endFence = raw.IndexOf("```", startFence + 3);
                if (endFence == -1)
                {
                    string blockRest = raw.Substring(startFence + 3).Trim();
                    list.Add(ParseCodeSegment(blockRest));
                    break;
                }

                string block = raw.Substring(startFence + 3, endFence - (startFence + 3)).Trim();
                list.Add(ParseCodeSegment(block));
                idx = endFence + 3;
            }
            return list;
        }

        private MessageSegment ParseCodeSegment(string block)
        {
            int nl = block.IndexOf('\n');
            string firstLine = nl > -1 ? block.Substring(0, nl).Trim() : block.Trim();
            string code = nl > -1 ? block.Substring(nl + 1).Trim() : "";

            if (firstLine.StartsWith("action:", StringComparison.OrdinalIgnoreCase))
            {
                string actionFull = firstLine.Substring(7);
                string actionType = actionFull;
                string actionParam = "";
                int colon = actionFull.IndexOf(':');
                if (colon > -1)
                {
                    actionType = actionFull.Substring(0, colon);
                    actionParam = actionFull.Substring(colon + 1);
                }
                return new MessageSegment
                {
                    IsCodeOrAction = true,
                    Language = "action",
                    Content = code,
                    ActionType = actionType.ToLowerInvariant(),
                    ActionParam = actionParam
                };
            }

            string lang = firstLine.ToLowerInvariant();
            if (lang == "powershell" || lang == "ps1" || lang == "cmd" || lang == "bat" || lang == "sh" || lang == "bash")
            {
                return new MessageSegment
                {
                    IsCodeOrAction = true,
                    Language = lang,
                    Content = string.IsNullOrEmpty(code) ? firstLine : code
                };
            }

            return new MessageSegment
            {
                IsCodeOrAction = true,
                Language = string.IsNullOrEmpty(firstLine) ? "powershell" : firstLine,
                Content = string.IsNullOrEmpty(code) ? firstLine : code
            };
        }

        private void AddMessageToChat(string message, bool isUser, bool isError = false, bool autoExecute = false)
        {
            var border = new Border
            {
                Background = isUser
                    ? new SolidColorBrush(Color.FromRgb(0x1F, 0x3A, 0x5C))
                    : isError
                        ? new SolidColorBrush(Color.FromRgb(0x3D, 0x14, 0x14))
                        : new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                BorderBrush = isUser
                    ? new SolidColorBrush(Color.FromRgb(0x38, 0x8B, 0xF8))
                    : isError
                        ? new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33))
                        : new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 720
            };

            var stackPanel = new StackPanel();

            // Header with icon and timestamp
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var icon = new IconBlock
            {
                Icon = isUser ? IconChar.User : isError ? IconChar.TriangleExclamation : IconChar.Robot,
                FontSize = 11,
                Foreground = isUser
                    ? new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF))
                    : isError
                        ? new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49))
                        : new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00)),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var nameText = new TextBlock
            {
                Text = isUser ? "You" : isError ? "Error" : "AI Assistant (VPS 103)",
                Foreground = isUser
                    ? new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF))
                    : isError
                        ? new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49))
                        : new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(nameText);
            headerPanel.Children.Add(timeText);
            stackPanel.Children.Add(headerPanel);

            if (isUser || isError)
            {
                var messageText = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC)),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 18
                };
                stackPanel.Children.Add(messageText);
            }
            else
            {
                // Parse segments (text vs code / action)
                var segments = ParseMessageSegments(message);
                bool hasCode = false;

                foreach (var seg in segments)
                {
                    if (!seg.IsCodeOrAction)
                    {
                        var textBlock = new TextBlock
                        {
                            Text = seg.Content,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC)),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap,
                            LineHeight = 18,
                            Margin = new Thickness(0, 0, 0, 6)
                        };
                        stackPanel.Children.Add(textBlock);
                    }
                    else
                    {
                        hasCode = true;
                        var card = CreateExecutionCard(seg, autoExecute);
                        stackPanel.Children.Add(card);
                    }
                }

                if (!hasCode && segments.Count == 0)
                {
                    var textBlock = new TextBlock
                    {
                        Text = message,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC)),
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 18
                    };
                    stackPanel.Children.Add(textBlock);
                }
            }

            border.Child = stackPanel;
            ChatMessagesPanel.Children.Add(border);
            ChatScrollViewer.ScrollToBottom();
        }

        private UIElement CreateExecutionCard(MessageSegment seg, bool autoExecute)
        {
            var cardBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 6, 0, 8)
            };

            var cardStack = new StackPanel();

            // Header
            var cardHeader = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            cardHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal };
            string titleText = seg.Language == "action" ? $"Aksi Langsung FiveM: {seg.ActionType}" : "PowerShell Execution";
            var headerIcon = new IconBlock
            {
                Icon = seg.Language == "action" ? IconChar.Bolt : IconChar.Terminal,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00)),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var headerText = new TextBlock
            {
                Text = titleText,
                Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titlePanel.Children.Add(headerIcon);
            titlePanel.Children.Add(headerText);
            Grid.SetColumn(titlePanel, 0);
            cardHeader.Children.Add(titlePanel);

            // Action Buttons Panel
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var btnCopy = new Button
            {
                Background = new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xD1, 0xD9)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 10,
                Cursor = Cursors.Hand,
                Content = "Salin"
            };
            btnCopy.Click += (s, e) =>
            {
                try
                {
                    Clipboard.SetText(seg.Content);
                    btnCopy.Content = "Tersalin!";
                }
                catch { }
            };

            var btnRun = new Button
            {
                Background = new SolidColorBrush(Color.FromRgb(0x23, 0x86, 0x36)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 3, 10, 3),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            var runStack = new StackPanel { Orientation = Orientation.Horizontal };
            runStack.Children.Add(new IconBlock { Icon = IconChar.Play, FontSize = 9, Foreground = Brushes.White, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center });
            runStack.Children.Add(new TextBlock { Text = seg.Language == "action" ? "Terapkan Sekarang" : "Jalankan di PowerShell", VerticalAlignment = VerticalAlignment.Center });
            btnRun.Content = runStack;

            btnPanel.Children.Add(btnCopy);
            btnPanel.Children.Add(btnRun);
            Grid.SetColumn(btnPanel, 1);
            cardHeader.Children.Add(btnPanel);
            cardStack.Children.Add(cardHeader);

            // Code Preview
            if (!string.IsNullOrEmpty(seg.Content))
            {
                var codeBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                var codeText = new TextBox
                {
                    Text = seg.Content,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0xE7, 0x87)),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap
                };
                codeBorder.Child = codeText;
                cardStack.Children.Add(codeBorder);
            }

            // Terminal Output Box
            var outputBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x05, 0x08, 0x0E)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x26, 0x2D)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 4, 0, 0)
            };
            var outputStack = new StackPanel();
            var statusHeader = new TextBlock
            {
                Text = "STATUS TERMINAL: Siap Dijalankan",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var outputContent = new TextBlock
            {
                Text = "(Klik tombol untuk mengeksekusi langsung pada mesin lokal)",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E)),
                TextWrapping = TextWrapping.Wrap
            };
            outputStack.Children.Add(statusHeader);
            outputStack.Children.Add(outputContent);
            outputBorder.Child = outputStack;
            cardStack.Children.Add(outputBorder);

            Func<Task> executeAction = async () =>
            {
                btnRun.IsEnabled = false;
                statusHeader.Text = "STATUS TERMINAL: Sedang Mengeksekusi...";
                statusHeader.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x00));
                outputContent.Text = "Menghubungkan ke engine eksekusi lokal...";
                outputContent.Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));

                try
                {
                    ExecutionResult res;
                    if (seg.Language == "action")
                    {
                        if (seg.ActionType.Contains("clean") || seg.ActionType.Contains("cache"))
                        {
                            res = await AiExecutionEngine.CleanFiveMCacheAsync();
                        }
                        else if (seg.ActionType.Contains("graphic") || seg.ActionType.Contains("preset"))
                        {
                            string pName = string.IsNullOrEmpty(seg.ActionParam) ? "competitive" : seg.ActionParam;
                            res = await AiExecutionEngine.ApplyGraphicsPresetAsync(pName);
                        }
                        else if (seg.ActionType.Contains("pool"))
                        {
                            res = await AiExecutionEngine.OptimizeCitizenFxIniAsync();
                        }
                        else if (seg.ActionType.Contains("perf") || seg.ActionType.Contains("high"))
                        {
                            res = await AiExecutionEngine.SetHighPerformancePowerPlanAsync();
                        }
                        else
                        {
                            res = await AiExecutionEngine.ExecutePowerShellAsync(seg.Content);
                        }
                    }
                    else
                    {
                        res = await AiExecutionEngine.ExecutePowerShellAsync(seg.Content);
                    }

                    if (res.Success)
                    {
                        statusHeader.Text = $"STATUS TERMINAL: ✓ Sukses Dijalankan (Exit Code: {res.ExitCode})";
                        statusHeader.Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43));
                        outputContent.Text = res.Output;
                        outputContent.Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF6, 0xFC));
                    }
                    else
                    {
                        statusHeader.Text = $"STATUS TERMINAL: ✗ Terjadi Kesalahan (Exit Code: {res.ExitCode})";
                        statusHeader.Foreground = new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));
                        outputContent.Text = string.IsNullOrEmpty(res.Output) ? res.ErrorMessage : res.Output;
                        outputContent.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
                    }
                }
                catch (Exception ex)
                {
                    statusHeader.Text = "STATUS TERMINAL: ✗ Exception";
                    statusHeader.Foreground = new SolidColorBrush(Color.FromRgb(0xDA, 0x36, 0x33));
                    outputContent.Text = ex.Message;
                    outputContent.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x51, 0x49));
                }
                finally
                {
                    btnRun.IsEnabled = true;
                    ChatScrollViewer.ScrollToBottom();
                }
            };

            btnRun.Click += async (s, e) => await executeAction();

            if (autoExecute)
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(200);
                    await executeAction();
                });
            }

            cardBorder.Child = cardStack;
            return cardBorder;
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Apakah Anda yakin ingin menghapus seluruh riwayat chat?",
                "Bersihkan Chat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ChatMessagesPanel.Children.Clear();
                _aiService.ClearHistory();
                AddWelcomeMessage();
            }
        }

        private async void BtnQuickCleanCache_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Bersihkan seluruh cache FiveM sekarang dan bebaskan ruang disk.", forceAutoRun: true);
        }

        private async void BtnQuickOptimizeFps_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Terapkan setting grafis gta5_settings.xml Competitive FPS untuk stabil di 144+ FPS.", forceAutoRun: true);
        }

        private async void BtnQuickPowerPlan_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Aktifkan Windows High Performance Power Plan dan flush DNS cache.", forceAutoRun: true);
        }

        private async void BtnQuickOptimizePools_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Tuning memory pool CitizenFX.ini untuk cegah crash custom map dan MLO.", forceAutoRun: true);
        }

        private async void BtnQuickOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Apa tips optimasi FiveM & GTA V terbaik untuk resolusi 1080p dengan NVIDIA RTX 5060 Ti?");
        }

        private async void BtnQuickRTX_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Berikan konfigurasi setting grafis optimal khusus NVIDIA RTX 5060 Ti.", forceAutoRun: true);
        }

        private async void BtnQuickLowFPS_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            await SendMessageAsync("Saya mengalami stutter di FiveM saat ramai. Jalankan diagnosa dan bersihkan cache untuk fix drop FPS.", forceAutoRun: true);
        }

        public void Refresh()
        {
            UpdateStatusDisplay();
        }
    }
}