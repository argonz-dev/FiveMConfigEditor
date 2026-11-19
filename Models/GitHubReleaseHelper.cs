using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FiveMConfigEditorWPF.Models
{
    public class ReleaseNotesInfo
    {
        public bool Ok { get; set; }
        public string TagName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Body { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
        public string PublishedAt { get; set; } = "";
        public string Error { get; set; } = "";
    }

    public static class GitHubReleaseHelper
    {
        private const string RepoApiBase = "https://api.github.com/repos/argonz-dev/FiveMConfigEditor/releases";

        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FiveMConfigEditorWPF");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        public static async Task<ReleaseNotesInfo> FetchReleaseNotesAsync(string? version)
        {
            var cleanVer = version?.Trim().TrimStart('v', 'V');
            var primaryUrl = string.IsNullOrWhiteSpace(cleanVer)
                ? $"{RepoApiBase}/latest"
                : $"{RepoApiBase}/tags/v{Uri.EscapeDataString(cleanVer)}";

            try
            {
                using var response = await Http.GetAsync(primaryUrl).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.NotFound && !string.IsNullOrWhiteSpace(cleanVer))
                {
                    using var fallbackResponse = await Http.GetAsync($"{RepoApiBase}/latest").ConfigureAwait(false);
                    return await ParseResponseAsync(fallbackResponse).ConfigureAwait(false);
                }

                return await ParseResponseAsync(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new ReleaseNotesInfo
                {
                    Ok = false,
                    Error = ex.Message
                };
            }
        }

        private static async Task<ReleaseNotesInfo> ParseResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return new ReleaseNotesInfo
                {
                    Ok = false,
                    Error = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})"
                };
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ReleaseNotesInfo
            {
                Ok = true,
                TagName = GetString(root, "tag_name"),
                Name = GetString(root, "name"),
                Body = GetString(root, "body"),
                HtmlUrl = GetString(root, "html_url"),
                PublishedAt = GetString(root, "published_at")
            };
        }

        private static string GetString(JsonElement obj, string propName)
        {
            return obj.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? ""
                : "";
        }
    }
}
