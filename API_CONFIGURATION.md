# 🔧 API Configuration - enowxai Integration

## Overview

Aplikasi ini menggunakan **enowxai** (self-hosted AI proxy) untuk AI Chat Assistant feature.

## API Details

### Endpoint Configuration
```
Base URL: http://localhost:1430/v1
Endpoint: /chat/completions
Method: POST
Format: OpenAI Compatible
```

### Authentication
```
Header: Authorization: Bearer YOUR_API_KEY
API Key: enx-b160143eb533f235fb19a59bfd8551eb98c564ff12c1bd0f3175fb26f8b88f65
```

### Model Configuration
```
Model: claude-sonnet-4.5
Temperature: 0.7
Max Tokens: 1000
Stream: false
```

## Available Models

Berdasarkan dokumentasi enowxai, model yang tersedia:

| Model | Description | Use Case |
|-------|-------------|----------|
| `claude-sonnet-4.5` | **Current** - Balanced performance | General chat, recommendations |
| `claude-sonnet-4` | Faster, lighter | Quick responses |
| `claude-opus-4.6` | Most capable | Complex analysis |

## Request Format

### OpenAI Compatible Format
```json
{
  "model": "claude-sonnet-4.5",
  "messages": [
    {
      "role": "system",
      "content": "You are an AI assistant..."
    },
    {
      "role": "user",
      "content": "Hello!"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 1000,
  "stream": false
}
```

### Response Format
```json
{
  "id": "chatcmpl-xxx",
  "object": "chat.completion",
  "created": 1778033000,
  "model": "claude-sonnet-4.5",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Response text here..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 0,
    "completion_tokens": 31,
    "total_tokens": 31,
    "credit": 0
  }
}
```

## Testing API Connection

### Using PowerShell
```powershell
$apiKey = "enx-b160143eb533f235fb19a59bfd8551eb98c564ff12c1bd0f3175fb26f8b88f65"
$body = @{
    model = "claude-sonnet-4.5"
    messages = @(
        @{
            role = "user"
            content = "Hello, test"
        }
    )
    max_tokens = 100
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:1430/v1/chat/completions" `
    -Method Post `
    -Headers @{
        "Content-Type" = "application/json"
        "Authorization" = "Bearer $apiKey"
    } `
    -Body $body
```

### Using curl
```bash
curl http://localhost:1430/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer enx-b160143eb533f235fb19a59bfd8551eb98c564ff12c1bd0f3175fb26f8b88f65" \
  -d '{
    "model": "claude-sonnet-4.5",
    "messages": [
      {"role": "user", "content": "Hello!"}
    ],
    "max_tokens": 100
  }'
```

## Implementation Details

### AiChatService.cs
```csharp
public AiChatService(string baseUrl, string apiKey)
{
    _baseUrl = baseUrl.TrimEnd('/');
    _apiKey = apiKey;
    _httpClient = new HttpClient
    {
        BaseAddress = new Uri(_baseUrl),
        Timeout = TimeSpan.FromSeconds(60)
    };
}

public async Task<string> SendMessageAsync(string userMessage)
{
    var requestBody = new
    {
        model = "claude-sonnet-4.5",
        messages = messages,
        temperature = 0.7,
        max_tokens = 1000,
        stream = false
    };
    
    var response = await _httpClient.PostAsync("/chat/completions", content);
    // ... parse response
}
```

### Key Points
1. ✅ **BaseAddress** set di HttpClient constructor
2. ✅ **Relative path** `/chat/completions` (bukan full URL)
3. ✅ **Model name** `claude-sonnet-4.5` (bukan `gpt-4o-mini`)
4. ✅ **Authorization** header dengan Bearer token

## Troubleshooting

### Error: "Invalid request URI"
**Cause:** BaseAddress tidak di-set atau path tidak relatif
**Solution:** 
- Set `HttpClient.BaseAddress = new Uri(baseUrl)`
- Use relative path: `/chat/completions`

### Error: "Model not found"
**Cause:** Model name tidak valid
**Solution:** 
- Use: `claude-sonnet-4.5` (recommended)
- Or: `claude-sonnet-4`, `claude-opus-4.6`

### Error: "Unauthorized"
**Cause:** API key salah atau tidak valid
**Solution:**
- Verify API key di enowxai dashboard
- Check Authorization header format

### Error: "Connection timeout"
**Cause:** enowxai service tidak berjalan
**Solution:**
- Start enowxai: `enowxai start`
- Check service status: `enowxai status`
- Verify port 1430 tidak digunakan aplikasi lain

## Advanced Configuration

### Change Model
Edit `AiChatService.cs`:
```csharp
model = "claude-opus-4.6"  // For more capable responses
```

### Enable Streaming
```csharp
var requestBody = new
{
    model = "claude-sonnet-4.5",
    messages = messages,
    stream = true  // Enable streaming
};
```

### Extended Thinking (for complex problems)
```csharp
var requestBody = new
{
    model = "claude-opus-4.6",
    messages = messages,
    reasoning_effort = "high",  // low, medium, high, max
    max_tokens = 4096
};
```

### Adjust Temperature
```csharp
temperature = 0.3  // More focused (0.0 - 1.0)
temperature = 0.9  // More creative
```

## enowxai Commands

### Start Service
```bash
enowxai start
```

### Check Status
```bash
enowxai status
```

### View API Key
```bash
enowxai apikey
```

### Access Dashboard
```
http://localhost:1431
```

## Security Notes

1. **API Key Storage**
   - Stored obfuscated in `SecureConfig.cs`
   - Base64 + XOR encryption
   - Not plaintext in binary

2. **Local Only**
   - API runs on localhost:1430
   - Not exposed to internet
   - Safe for local development

3. **Rate Limiting**
   - Check enowxai documentation
   - May have usage limits
   - Monitor credit usage

## Performance

### Response Times
- Simple queries: 2-5 seconds
- Complex analysis: 5-15 seconds
- Config analysis: 10-20 seconds

### Optimization Tips
1. Use `claude-sonnet-4` for faster responses
2. Reduce `max_tokens` for shorter responses
3. Lower `temperature` for more focused answers
4. Enable streaming for better UX (future feature)

## References

- **enowxai Documentation**: Check local docs
- **API Endpoint**: `http://localhost:1430/v1`
- **Dashboard**: `http://localhost:1431`
- **Model**: claude-sonnet-4.5

---

**Last Updated**: 2026-05-06
**API Version**: v1
**Status**: ✅ Working
