using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FiveMConfigEditorWPF.Models
{
    public class AiChatMessage
    {
        public string Role { get; set; } = "user"; // "user" or "assistant"
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class AiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly List<AiChatMessage> _conversationHistory;

        public string BaseUrl => _baseUrl;
        public string Model => _model;

        public AiChatService(string? baseUrl = null, string? apiKey = null, string? model = null)
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? SecureConfig.GetApiBaseUrl() : baseUrl.TrimEnd('/');
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? SecureConfig.GetApiKey() : apiKey.Trim();
            _model = string.IsNullOrWhiteSpace(model) ? SecureConfig.GetDefaultModel() : model.Trim();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            _conversationHistory = new List<AiChatMessage>();

            // Add system message for context
            _conversationHistory.Add(new AiChatMessage
            {
                Role = "system",
                Content = @"Kamu adalah AI Assistant resmi FiveM Config Editor yang berjalan langsung di PC pengguna (Windows 11).
PENGGUNA MENGHARAPKAN EKSEKUSI LANGSUNG DAN OTOMATIS, BUKAN INSTRUKSI MANUAL.

KEMAMPUAN SISTEM & HAK AKSES EKSEKUSI:
1. Kamu terintegrasi erat dengan FiveM Config Editor dan MEMILIKI AKSES EKSEKUSI POWERSHELL LOKAL SERTA PENGATURAN FIVEM SECARA NYATA.
2. JANGAN PERNAH mengatakan 'Tidak bisa. Tidak ada akses shell ke PC lokal' atau menyuruh pengguna copy-paste sendiri jika mereka minta dijalankan.
3. Ketika pengguna meminta menjalankan sesuatu di PowerShell, membersihkan cache, atau optimasi game/PC:
   - Buat blok kode PowerShell executable yang valid:
```powershell
<perintah powershell di sini>
```
   - Atau gunakan blok aksi langsung FiveM:
```action:clean_cache
```
```action:optimize_graphics:competitive
```
```action:optimize_graphics:rtx
```
```action:optimize_pools
```
```action:high_performance
```
   Aplikasi FiveM Config Editor akan otomatis mengeksekusi blok ini di mesin lokal pengguna dan menampilkan hasil output terminalnya secara langsung di layar chat.
4. Spesifikasi Hardware Terdeteksi: NVIDIA GeForce RTX 5060 Ti, Windows 11.
5. Konfigurasi:
   - gta5_settings.xml: %AppData%\CitizenFX\gta5_settings.xml
   - CitizenFX.ini: folder FiveM.app (pools, channel, pools memory)
   - Cache FiveM: folder FiveM.app\data\cache, server-cache, nui-storage.

GAYA KOMUNIKASI:
- Singkat, padat, berorientasi tindakan (telegraphic / direct action).
- Bahasa Indonesia yang santai, lugas, dan solutif.
- Tuliskan tindakan yang diambil dan langsung berikan blok eksekusinya."
            });
        }

        public List<AiChatMessage> GetConversationHistory()
        {
            return _conversationHistory.FindAll(m => m.Role != "system");
        }

        public void ClearHistory()
        {
            var systemMessage = _conversationHistory[0];
            _conversationHistory.Clear();
            _conversationHistory.Add(systemMessage);
        }

        public async Task<string> SendMessageAsync(string userMessage, string? contextData = null)
        {
            try
            {
                _conversationHistory.Add(new AiChatMessage
                {
                    Role = "user",
                    Content = userMessage,
                    Timestamp = DateTime.Now
                });

                string messageWithContext = userMessage;
                if (!string.IsNullOrEmpty(contextData))
                {
                    messageWithContext = $"{userMessage}\n\nContext:\n{contextData}";
                }

                var messages = new List<object>();
                foreach (var msg in _conversationHistory)
                {
                    messages.Add(new
                    {
                        role = msg.Role,
                        content = msg.Role == "user" && msg.Content == userMessage 
                            ? messageWithContext 
                            : msg.Content
                    });
                }

                var requestBody = new
                {
                    model = _model,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 2000,
                    stream = false
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                string endpoint = _baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                    ? $"{_baseUrl}/chat/completions"
                    : $"{_baseUrl}/v1/chat/completions";

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };

                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                }

                var response = await _httpClient.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: HTTP {(int)response.StatusCode} - {responseString}";
                }

                using var jsonDoc = JsonDocument.Parse(responseString);
                var root = jsonDoc.RootElement;
                string assistantMessage = "";

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var msgObj) && msgObj.TryGetProperty("content", out var contentProp))
                    {
                        assistantMessage = contentProp.GetString() ?? "";
                    }
                    else if (firstChoice.TryGetProperty("text", out var textProp))
                    {
                        assistantMessage = textProp.GetString() ?? "";
                    }
                }

                if (string.IsNullOrWhiteSpace(assistantMessage))
                {
                    assistantMessage = "(Tidak ada respons teks dari LLM provider)";
                }

                _conversationHistory.Add(new AiChatMessage
                {
                    Role = "assistant",
                    Content = assistantMessage,
                    Timestamp = DateTime.Now
                });

                return assistantMessage;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public static async Task<(bool Success, string Message)> TestConnectionAsync(string baseUrl, string apiKey, string model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return (false, "Base URL tidak boleh kosong.");

                var cleanUrl = baseUrl.TrimEnd('/');
                string endpoint = cleanUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                    ? $"{cleanUrl}/chat/completions"
                    : $"{cleanUrl}/v1/chat/completions";

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                var payload = new
                {
                    model = string.IsNullOrWhiteSpace(model) ? "gemini-3.8-flash-high" : model,
                    messages = new[]
                    {
                        new { role = "user", content = "ping" }
                    },
                    max_tokens = 10
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                }

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, $"HTTP {(int)response.StatusCode}: {body}");
                }

                return (true, "Terhubung ke LLM provider berhasil!");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
