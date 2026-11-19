using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FiveMConfigEditorWPF.Models
{
    public class ServerDynamicInfo
    {
        public bool Ok { get; set; }
        public int Clients { get; set; }
        public int MaxClients { get; set; }
        public string Hostname { get; set; } = "";
        public string GameBuild { get; set; } = "";
        public int ResourceCount { get; set; }
        public string GameType { get; set; } = "";
        public string Error { get; set; } = "";
    }

    public class ServerNetResult
    {
        public bool Ok { get; set; }
        public string Host { get; set; } = "";
        public int Sent { get; set; }
        public int Recv { get; set; }
        public double AvgMs { get; set; }
        public double MinMs { get; set; }
        public double MaxMs { get; set; }
        public double JitterMs { get; set; }
        public double LossPercent { get; set; }
        public string Rating { get; set; } = "";
        public int Level { get; set; }
        public string Error { get; set; } = "";
    }

    public static class ServerProbeHelper
    {
        private const int DefaultFiveMPort = 30120;

        private static readonly HttpClient Http = CreateHttpClient();
        private static readonly Regex CfxJoinRegex = new(
            @"cfx\.re/join/([A-Za-z0-9]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex FiveMColorRegex = new(
            @"\^[0-9]|~[^~]{1,4}~",
            RegexOptions.Compiled);
        private static readonly byte[] FiveMGetInfoPacket =
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }
            .Concat(Encoding.ASCII.GetBytes("getinfo fiveconfig"))
            .ToArray();

        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                ConnectCallback = async (ctx, token) =>
                {
                    var addrs = await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, token).ConfigureAwait(false);
                    var ordered = addrs
                        .OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                        .ToArray();

                    Exception? lastEx = null;
                    foreach (var ip in ordered)
                    {
                        var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                        try
                        {
                            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                            connectCts.CancelAfter(TimeSpan.FromSeconds(3));
                            await socket.ConnectAsync(ip, ctx.DnsEndPoint.Port, connectCts.Token).ConfigureAwait(false);
                            return new NetworkStream(socket, ownsSocket: true);
                        }
                        catch (Exception ex)
                        {
                            socket.Dispose();
                            lastEx = ex;
                        }
                    }
                    throw lastEx ?? new SocketException((int)SocketError.HostUnreachable);
                }
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FiveMConfigEditor/1.0");
            return client;
        }

        public static async Task<(bool Ok, string Host, int Port, string Error)> ResolveEndpointAsync(
            string serverUrl,
            CancellationToken ct = default)
        {
            string raw = (serverUrl ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw))
                return (false, "", 0, "Alamat server kosong.");

            var cfxMatch = CfxJoinRegex.Match(raw);
            if (cfxMatch.Success)
            {
                string code = cfxMatch.Groups[1].Value;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(5));
                    string apiUrl = $"https://servers-frontend.fivem.net/api/servers/single/{code}";
                    using var resp = await Http.GetAsync(apiUrl, cts.Token).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                        return (false, "", 0, $"Gagal resolve cfx.re/join/{code} (HTTP {(int)resp.StatusCode}).");

                    using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);

                    if (doc.RootElement.TryGetProperty("Data", out var dataEl) &&
                        dataEl.TryGetProperty("connectEndPoints", out var epsEl) &&
                        epsEl.ValueKind == JsonValueKind.Array &&
                        epsEl.GetArrayLength() > 0)
                    {
                        string? ep = epsEl[0].GetString();
                        if (!string.IsNullOrWhiteSpace(ep) && TryParseHostPort(ep, out string h, out int p))
                            return (true, h, p, "");
                    }

                    return (false, "", 0, $"Endpoint server untuk cfx.re/join/{code} tidak ditemukan.");
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    return (false, "", 0, "Timeout saat resolve cfx.re.");
                }
                catch (Exception ex)
                {
                    return (false, "", 0, $"Gagal resolve cfx.re: {ex.Message}");
                }
            }

            if (raw.StartsWith("fivem://connect/", StringComparison.OrdinalIgnoreCase))
                raw = raw["fivem://connect/".Length..];

            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
                {
                    int port = uri.IsDefaultPort ? DefaultFiveMPort : uri.Port;
                    return (true, uri.Host, port, "");
                }
                return (false, "", 0, "Format URL server tidak valid.");
            }

            int qIdx = raw.IndexOf('?');
            if (qIdx >= 0) raw = raw[..qIdx];
            raw = raw.TrimEnd('/');

            if (TryParseHostPort(raw, out string host, out int parsedPort))
                return (true, host, parsedPort, "");

            return (false, "", 0, "Format alamat server tidak valid (gunakan host:port, fivem://connect/..., atau cfx.re/join/...).");
        }

        private static bool TryParseHostPort(string input, out string host, out int port)
        {
            host = "";
            port = DefaultFiveMPort;
            string s = (input ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return false;

            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(s, UriKind.Absolute, out var u) && !string.IsNullOrWhiteSpace(u.Host))
                {
                    host = u.Host;
                    port = u.IsDefaultPort ? DefaultFiveMPort : u.Port;
                    return true;
                }
                return false;
            }

            if (Uri.TryCreate($"tcp://{s}", UriKind.Absolute, out var tcpUri) && !string.IsNullOrWhiteSpace(tcpUri.Host))
            {
                host = tcpUri.Host;
                port = tcpUri.Port > 0 ? tcpUri.Port : DefaultFiveMPort;
                return port is >= 1 and <= 65535;
            }

            return false;
        }

        public static async Task<ServerDynamicInfo> FetchDynamicAsync(
            string serverUrl,
            CancellationToken ct = default)
        {
            string raw = (serverUrl ?? string.Empty).Trim();
            var cfxMatch = CfxJoinRegex.Match(raw);
            if (cfxMatch.Success)
            {
                var cfxInfo = await FetchCfxSingleAsync(cfxMatch.Groups[1].Value, ct).ConfigureAwait(false);
                if (cfxInfo.Ok) return cfxInfo;
            }

            var (ok, host, port, err) = await ResolveEndpointAsync(raw, ct).ConfigureAwait(false);
            if (!ok)
                return new ServerDynamicInfo { Ok = false, Error = err };

            try
            {
                string baseUrl = $"http://{FormatHostForUri(host)}:{port}";
                var dynTask = FetchDynamicOnlyAsync($"{baseUrl}/dynamic.json", ct);
                var infoTask = TryFetchInfoJsonAsync($"{baseUrl}/info.json", ct);
                await Task.WhenAll(dynTask, infoTask).ConfigureAwait(false);

                var dyn = dynTask.Result;
                if (!dyn.Ok) return dyn;

                var (build, resCount) = infoTask.Result;
                dyn.GameBuild = build;
                dyn.ResourceCount = resCount;
                return dyn;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return new ServerDynamicInfo { Ok = false, Error = "Timeout mengambil dynamic.json (6s)." };
            }
            catch (Exception ex)
            {
                return new ServerDynamicInfo { Ok = false, Error = ex.Message };
            }
        }

        private static async Task<ServerDynamicInfo> FetchDynamicOnlyAsync(string url, CancellationToken ct)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(6));

                using var resp = await Http.GetAsync(url, cts.Token).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    return new ServerDynamicInfo { Ok = false, Error = $"HTTP {(int)resp.StatusCode}" };

                using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
                var root = doc.RootElement;

                int clients = ReadFlexibleInt(root, "clients");
                int maxClients = ReadFlexibleInt(root, "sv_maxclients");
                string hostname = root.TryGetProperty("hostname", out var hn) && hn.ValueKind == JsonValueKind.String
                    ? CleanFiveMText(hn.GetString())
                    : "";
                string gameType = root.TryGetProperty("gametype", out var gt) && gt.ValueKind == JsonValueKind.String
                    ? CleanFiveMText(gt.GetString())
                    : "";

                return new ServerDynamicInfo
                {
                    Ok = true,
                    Clients = clients,
                    MaxClients = maxClients,
                    Hostname = hostname,
                    GameType = gameType
                };
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return new ServerDynamicInfo { Ok = false, Error = "Timeout mengambil dynamic.json (6s)." };
            }
            catch (Exception ex)
            {
                return new ServerDynamicInfo { Ok = false, Error = ex.Message };
            }
        }

        private static async Task<ServerDynamicInfo> FetchCfxSingleAsync(string code, CancellationToken ct)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                using var resp = await Http.GetAsync($"https://servers-frontend.fivem.net/api/servers/single/{code}", cts.Token).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    return new ServerDynamicInfo { Ok = false, Error = $"HTTP {(int)resp.StatusCode}" };

                using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
                if (!doc.RootElement.TryGetProperty("Data", out var data))
                    return new ServerDynamicInfo { Ok = false, Error = "Data kosong." };

                int clients = ReadFlexibleInt(data, "clients");
                int maxClients = ReadFlexibleInt(data, "sv_maxclients");
                string hostname = data.TryGetProperty("hostname", out var hn) && hn.ValueKind == JsonValueKind.String
                    ? CleanFiveMText(hn.GetString())
                    : "";
                string gameType = data.TryGetProperty("gametype", out var gt) && gt.ValueKind == JsonValueKind.String
                    ? CleanFiveMText(gt.GetString())
                    : "";
                int resCount = data.TryGetProperty("resources", out var res) && res.ValueKind == JsonValueKind.Array
                    ? res.GetArrayLength()
                    : 0;
                string build = ReadVarsGameBuild(data);

                return new ServerDynamicInfo
                {
                    Ok = true,
                    Clients = clients,
                    MaxClients = maxClients,
                    Hostname = hostname,
                    GameBuild = build,
                    ResourceCount = resCount,
                    GameType = gameType
                };
            }
            catch (Exception ex)
            {
                return new ServerDynamicInfo { Ok = false, Error = ex.Message };
            }
        }

        private static async Task<(string Build, int ResourceCount)> TryFetchInfoJsonAsync(string url, CancellationToken ct)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(4));
                using var resp = await Http.GetAsync(url, cts.Token).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return ("", 0);
                using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
                var root = doc.RootElement;
                int resCount = root.TryGetProperty("resources", out var res) && res.ValueKind == JsonValueKind.Array
                    ? res.GetArrayLength()
                    : 0;
                string build = ReadVarsGameBuild(root);
                return (build, resCount);
            }
            catch { return ("", 0); }
        }

        private static string ReadVarsGameBuild(JsonElement obj)
        {
            if (!obj.TryGetProperty("vars", out var vars) || vars.ValueKind != JsonValueKind.Object)
                return "";
            if (!vars.TryGetProperty("sv_enforceGameBuild", out var gb))
                return "";
            if (gb.ValueKind == JsonValueKind.String)
                return gb.GetString()?.Trim() ?? "";
            if (gb.ValueKind == JsonValueKind.Number)
                return gb.GetRawText();
            return "";
        }

        public static async Task<(bool Ok, string? IconPath, string? BannerPath, string Error)> FetchAndSaveServerAssetsAsync(
            string serverUrl,
            string presetId,
            CancellationToken ct = default)
        {
            string raw = (serverUrl ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(raw))
                return (false, null, null, "Alamat server kosong.");

            string? iconBase64 = null;
            string? iconHttpUrl = null;
            string? bannerUrl = null;

            var cfxMatch = CfxJoinRegex.Match(raw);
            if (cfxMatch.Success)
            {
                string code = cfxMatch.Groups[1].Value;
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(6));
                    using var resp = await Http.GetAsync($"https://servers-frontend.fivem.net/api/servers/single/{code}", cts.Token).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
                        if (doc.RootElement.TryGetProperty("Data", out var data))
                        {
                            if (data.TryGetProperty("icon", out var ic) && ic.ValueKind == JsonValueKind.String)
                                iconBase64 = ic.GetString();
                            if (string.IsNullOrWhiteSpace(iconBase64) &&
                                data.TryGetProperty("iconVersion", out var iv) &&
                                (iv.ValueKind == JsonValueKind.Number || iv.ValueKind == JsonValueKind.String))
                            {
                                string ivStr = iv.ValueKind == JsonValueKind.Number ? iv.GetRawText() : (iv.GetString() ?? "");
                                if (!string.IsNullOrWhiteSpace(ivStr))
                                    iconHttpUrl = $"https://frontend.cfx-services.net/api/servers/icon/{code}/{ivStr}.png";
                            }
                            bannerUrl = ReadBannerUrl(data);
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(iconBase64) || string.IsNullOrWhiteSpace(bannerUrl))
            {
                var (ok, host, port, err) = await ResolveEndpointAsync(raw, ct).ConfigureAwait(false);
                if (ok)
                {
                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(TimeSpan.FromSeconds(6));
                        using var resp = await Http.GetAsync($"http://{FormatHostForUri(host)}:{port}/info.json", cts.Token).ConfigureAwait(false);
                        if (resp.IsSuccessStatusCode)
                        {
                            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token).ConfigureAwait(false);
                            var root = doc.RootElement;
                            if (string.IsNullOrWhiteSpace(iconBase64) &&
                                root.TryGetProperty("icon", out var ic) && ic.ValueKind == JsonValueKind.String)
                            {
                                iconBase64 = ic.GetString();
                            }
                            if (string.IsNullOrWhiteSpace(bannerUrl))
                                bannerUrl = ReadBannerUrl(root);
                        }
                    }
                    catch { }
                }
                else if (string.IsNullOrWhiteSpace(iconBase64) && string.IsNullOrWhiteSpace(iconHttpUrl) && string.IsNullOrWhiteSpace(bannerUrl))
                {
                    return (false, null, null, err);
                }
            }

            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server_assets");
            Directory.CreateDirectory(assetsDir);
            string safeId = Regex.Replace(string.IsNullOrWhiteSpace(presetId) ? "preset" : presetId, @"[^A-Za-z0-9_-]", "_");

            string? savedIconPath = null;
            if (!string.IsNullOrWhiteSpace(iconBase64))
            {
                try
                {
                    int commaIdx = iconBase64.IndexOf(',');
                    string b64 = commaIdx >= 0 ? iconBase64[(commaIdx + 1)..] : iconBase64;
                    byte[] bytes = Convert.FromBase64String(b64.Trim());
                    if (bytes.Length > 16)
                    {
                        string iconPath = Path.Combine(assetsDir, $"{safeId}_icon.png");
                        await File.WriteAllBytesAsync(iconPath, bytes, ct).ConfigureAwait(false);
                        savedIconPath = iconPath;
                    }
                }
                catch { }
            }
            else if (!string.IsNullOrWhiteSpace(iconHttpUrl))
            {
                savedIconPath = await DownloadImageAsync(iconHttpUrl, Path.Combine(assetsDir, $"{safeId}_icon"), ".png", ct).ConfigureAwait(false);
            }

            string? savedBannerPath = null;
            if (!string.IsNullOrWhiteSpace(bannerUrl))
            {
                savedBannerPath = await DownloadImageAsync(bannerUrl, Path.Combine(assetsDir, $"{safeId}_banner"), ".png", ct).ConfigureAwait(false);
            }

            bool anySaved = savedIconPath != null || savedBannerPath != null;
            return (anySaved, savedIconPath, savedBannerPath, anySaved ? "" : "Server tidak menyediakan icon/banner.");
        }

        private static string? ReadBannerUrl(JsonElement obj)
        {
            if (!obj.TryGetProperty("vars", out var vars) || vars.ValueKind != JsonValueKind.Object)
                return null;
            foreach (var key in new[] { "banner_detail", "banner_connecting" })
            {
                if (vars.TryGetProperty(key, out var b) && b.ValueKind == JsonValueKind.String)
                {
                    string? u = b.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(u) &&
                        (u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                         u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
                    {
                        return u;
                    }
                }
            }
            return null;
        }

        private static async Task<string?> DownloadImageAsync(string url, string basePathNoExt, string defaultExt, CancellationToken ct)
        {
            const int MaxBytes = 15 * 1024 * 1024;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return null;

                long? len = resp.Content.Headers.ContentLength;
                if (len.HasValue && len.Value > MaxBytes) return null;

                string ext = defaultExt;
                string? mediaType = resp.Content.Headers.ContentType?.MediaType;
                if (string.Equals(mediaType, "image/gif", StringComparison.OrdinalIgnoreCase)) ext = ".gif";
                else if (string.Equals(mediaType, "image/jpeg", StringComparison.OrdinalIgnoreCase)) ext = ".jpg";
                else if (string.Equals(mediaType, "image/webp", StringComparison.OrdinalIgnoreCase)) ext = ".webp";
                else if (Uri.TryCreate(url, UriKind.Absolute, out var u))
                {
                    string pathExt = Path.GetExtension(u.AbsolutePath).ToLowerInvariant();
                    if (pathExt is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp")
                        ext = pathExt == ".jpeg" ? ".jpg" : pathExt;
                }

                await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
                using var ms = new MemoryStream();
                byte[] buffer = new byte[16384];
                int read;
                while ((read = await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false)) > 0)
                {
                    if (ms.Length + read > MaxBytes) return null;
                    ms.Write(buffer, 0, read);
                }
                if (ms.Length < 16) return null;

                string outPath = basePathNoExt + ext;
                await File.WriteAllBytesAsync(outPath, ms.ToArray(), ct).ConfigureAwait(false);
                return outPath;
            }
            catch { return null; }
        }

        private static string CleanFiveMText(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            return FiveMColorRegex.Replace(raw, "").Trim();
        }

        public static async Task<ServerNetResult> ProbeNetworkAsync(
            string serverUrl,
            int rounds = 8,
            IProgress<(int Done, int Total, double? LastMs)>? progress = null,
            CancellationToken ct = default)
        {
            int totalRounds = Math.Max(1, rounds);
            var (ok, host, port, err) = await ResolveEndpointAsync(serverUrl, ct).ConfigureAwait(false);
            if (!ok)
            {
                return new ServerNetResult
                {
                    Ok = false,
                    Rating = "Tidak Terhubung",
                    Level = 0,
                    Error = err
                };
            }

            IPAddress targetIp;
            try
            {
                var addrs = await Dns.GetHostAddressesAsync(host, ct).ConfigureAwait(false);
                if (addrs.Length == 0)
                {
                    return new ServerNetResult
                    {
                        Ok = false,
                        Host = host,
                        Rating = "Tidak Terhubung",
                        Level = 0,
                        Error = $"DNS gagal resolve '{host}'."
                    };
                }
                targetIp = addrs.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addrs[0];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new ServerNetResult
                {
                    Ok = false,
                    Host = host,
                    Rating = "Tidak Terhubung",
                    Level = 0,
                    Error = $"DNS error: {ex.Message}"
                };
            }

            var tcpRtts = new List<double>(totalRounds);
            var udpRtts = new List<double>(totalRounds);
            int tcpRecv = 0;
            int udpRecv = 0;

            for (int i = 0; i < totalRounds; i++)
            {
                ct.ThrowIfCancellationRequested();

                var tcpTask = ProbeTcpConnectAsync(targetIp, port, ct);
                var udpTask = ProbeUdpGetInfoAsync(targetIp, port, ct);
                await Task.WhenAll(tcpTask, udpTask).ConfigureAwait(false);

                double? tcpMs = tcpTask.Result;
                double? udpMs = udpTask.Result;

                if (tcpMs.HasValue)
                {
                    tcpRecv++;
                    tcpRtts.Add(tcpMs.Value);
                }
                if (udpMs.HasValue)
                {
                    udpRecv++;
                    udpRtts.Add(udpMs.Value);
                }

                progress?.Report((i + 1, totalRounds, tcpMs ?? udpMs));

                if (i < totalRounds - 1)
                    await Task.Delay(250, ct).ConfigureAwait(false);
            }

            var rtts = tcpRtts.Count > 0 ? tcpRtts : udpRtts;
            if (rtts.Count == 0)
            {
                return new ServerNetResult
                {
                    Ok = false,
                    Host = host,
                    Sent = totalRounds,
                    Recv = 0,
                    LossPercent = 100.0,
                    Rating = "Tidak Terhubung",
                    Level = 0,
                    Error = "Server tidak merespons (TCP & UDP timeout)."
                };
            }

            int effectiveRecv = udpRecv > 0 ? udpRecv : tcpRecv;
            double lossPercent = Math.Round((totalRounds - effectiveRecv) * 100.0 / totalRounds, 1);
            double avg = rtts.Average();
            double min = rtts.Min();
            double max = rtts.Max();

            double jitter = 0.0;
            if (rtts.Count > 1)
            {
                double diffSum = 0.0;
                for (int k = 1; k < rtts.Count; k++)
                    diffSum += Math.Abs(rtts[k] - rtts[k - 1]);
                jitter = diffSum / (rtts.Count - 1);
            }

            var (rating, level) = ClassifyQuality(lossPercent, avg);

            return new ServerNetResult
            {
                Ok = true,
                Host = host,
                Sent = totalRounds,
                Recv = effectiveRecv,
                AvgMs = Math.Round(avg, 1),
                MinMs = Math.Round(min, 1),
                MaxMs = Math.Round(max, 1),
                JitterMs = Math.Round(jitter, 1),
                LossPercent = lossPercent,
                Rating = rating,
                Level = level
            };
        }

        private static async Task<double?> ProbeTcpConnectAsync(IPAddress ip, int port, CancellationToken ct)
        {
            using var tcp = new TcpClient(ip.AddressFamily);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(2000);
            var sw = Stopwatch.StartNew();
            try
            {
                await tcp.ConnectAsync(ip, port, cts.Token).ConfigureAwait(false);
                sw.Stop();
                return sw.Elapsed.TotalMilliseconds;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<double?> ProbeUdpGetInfoAsync(IPAddress ip, int port, CancellationToken ct)
        {
            using var udp = new UdpClient(ip.AddressFamily);
            try
            {
                udp.Connect(ip, port);
                var sw = Stopwatch.StartNew();
                await udp.SendAsync(FiveMGetInfoPacket, FiveMGetInfoPacket.Length).ConfigureAwait(false);
                var recvTask = udp.ReceiveAsync(ct).AsTask();
                var res = await recvTask.WaitAsync(TimeSpan.FromMilliseconds(900), ct).ConfigureAwait(false);
                sw.Stop();
                return res.Buffer.Length > 0 ? sw.Elapsed.TotalMilliseconds : null;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return null;
            }
            catch (TimeoutException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }

        private static (string Rating, int Level) ClassifyQuality(double lossPercent, double avgMs)
        {
            if (lossPercent > 15.0 || avgMs >= 150.0)
                return ("Buruk", 0);
            if (lossPercent > 6.0 || avgMs >= 80.0)
                return ("Sedang", 1);
            if (lossPercent > 2.0 || avgMs >= 30.0)
                return ("Baik", 2);
            return ("Sangat Baik", 3);
        }

        private static string FormatHostForUri(string host)
        {
            return host.Contains(':') && !host.StartsWith('[') ? $"[{host}]" : host;
        }

        private static int ReadFlexibleInt(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var el))
                return 0;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int num))
                return num;
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out int parsed))
                return parsed;
            return 0;
        }
    }
}
