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
        private readonly List<AiChatMessage> _conversationHistory;

        public AiChatService(string baseUrl, string apiKey)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:1430";
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = "enx-b160143eb533f235fb19a59bfd8551eb98c564ff12c1bd0f3175fb26f8b88f65";
            }
            
            _baseUrl = baseUrl.TrimEnd('/'); // Remove trailing slash if exists
            _apiKey = apiKey;
            
            try
            {
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri(_baseUrl),
                    Timeout = TimeSpan.FromSeconds(60)
                };
            }
            catch
            {
                // Fallback if URI creation fails
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:1430"),
                    Timeout = TimeSpan.FromSeconds(60)
                };
            }
            
            _conversationHistory = new List<AiChatMessage>();

            // Add system message for context
            _conversationHistory.Add(new AiChatMessage
            {
                Role = "system",
                Content = @"You are an AI assistant specialized in GTA V and FiveM configuration optimization.

You help users optimize their graphics settings, troubleshoot performance issues, and provide recommendations based on their hardware specifications.

IMPORTANT FILE LOCATIONS:
- CitizenFX.ini: Located in FiveM installation folder (e.g., D:\FiveM\FiveM.app\)
  Contains FiveM-specific settings like pool sizes, build number, ReShade settings
  
- gta5_settings.xml: Located in C:\Users\[username]\AppData\Roaming\CitizenFX\
  Contains GTA V graphics settings like texture quality, shader quality, shadow quality, reflection quality, 
  water quality, particles quality, grass quality, soft shadows, post FX, motion blur, depth of field, 
  anisotropic filtering, MSAA, TXAA, FXAA, resolution, vsync, population density, distance scaling, etc.

- Mods folder: Located in FiveM installation folder (e.g., D:\FiveM\FiveM.app\mods\)
  Contains game modification files like:
  - .rpf files (game archives containing textures, models, etc.)
  - update.rpf, x64e.rpf, etc.
  - Visual mods like QuantV, NaturalVision, Redux
  - Vehicle mods, weapon mods, map mods

- Plugins folder: Located in FiveM installation folder (e.g., D:\FiveM\FiveM.app\plugins\)
  Contains ASI plugins and DLL files like:
  - ScriptHookV.dll
  - dinput8.dll, dxgi.dll (ASI loaders)
  - ReShade files
  - ENB files
  - Other enhancement plugins

- Server list: Located in C:\Users\[username]\AppData\Roaming\CitizenFX\
  File: servers.json or favorites.json
  Contains saved/favorite FiveM servers with IP addresses, ports, and server names

FOLDER STRUCTURE UNDERSTANDING:
When users mention 'mods' or ask about visual enhancements, you should reference the mods folder.
When users mention 'plugins', 'ASI', 'ReShade', or 'ENB', you should reference the plugins folder.
When users ask about servers or server list, you should reference the CitizenFX AppData folder.

When users ask about graphics settings or performance optimization, you should reference gta5_settings.xml.
When users ask about FiveM configuration, pool sizes, or ReShade, you should reference CitizenFX.ini.

Always be helpful, concise, and technical when needed. Respond in the same language as the user's question."
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
                // Add user message to history
                _conversationHistory.Add(new AiChatMessage
                {
                    Role = "user",
                    Content = userMessage,
                    Timestamp = DateTime.Now
                });

                // Add context if provided
                string messageWithContext = userMessage;
                if (!string.IsNullOrEmpty(contextData))
                {
                    messageWithContext = $"{userMessage}\n\nContext:\n{contextData}";
                }

                // Prepare request
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
                    model = "claude-sonnet-4.5", // Changed from gpt-4o-mini to claude-sonnet-4.5
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 1000,
                    stream = false // Non-streaming for simplicity
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync("/v1/chat/completions", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: {response.StatusCode} - {responseString}";
                }

                var jsonResponse = JsonDocument.Parse(responseString);
                var assistantMessage = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "No response";

                // Add assistant response to history
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

        public async Task<string> AnalyzeConfigAsync(Dictionary<string, string> configData)
        {
            var configText = new StringBuilder();
            configText.AppendLine("Current FiveM Configuration:");
            foreach (var kvp in configData)
            {
                configText.AppendLine($"{kvp.Key} = {kvp.Value}");
            }

            return await SendMessageAsync(
                "Please analyze this FiveM configuration and provide optimization recommendations:",
                configText.ToString()
            );
        }

        public async Task<string> GetOptimizationSuggestionAsync(string gpuModel, string targetFps, string resolution)
        {
            var prompt = $@"I have the following setup:
- GPU: {gpuModel}
- Target FPS: {targetFps}
- Resolution: {resolution}

Please suggest optimal GTA V graphics settings for FiveM to achieve this target FPS while maintaining good visual quality.";

            return await SendMessageAsync(prompt);
        }
    }
}
