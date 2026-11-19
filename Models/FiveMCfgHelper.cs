using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FiveMConfigEditorWPF.Models
{
    public class FiveMKeybind
    {
        public string Mapper { get; set; } = "KEYBOARD";
        public string Key { get; set; } = "";
        public string Command { get; set; } = "";
        public bool Disabled { get; set; }
    }

    public static class FiveMCfgHelper
    {
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
        private static readonly Regex NicknameLineRegex = new(
            @"^\s*(?:seta\s+)?(?:""cl_nickname""|cl_nickname)\s+(?:""([^""]*)""|(\S+))\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NicknameMatchRegex = new(
            @"^\s*(?:seta\s+)?(?:""cl_nickname""|cl_nickname)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AnyBindPrefixRegex = new(
            @"^\s*bind\s+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TokenRegex = new(
            @"^[A-Za-z0-9_]{1,32}$",
            RegexOptions.Compiled);

        private static readonly byte[] SstMagic = { 0x57, 0xFB, 0x80, 0x8B, 0x24, 0x75, 0x47, 0xDB };
        private static readonly byte[] NuiOriginBytes = Encoding.ASCII.GetBytes("_https://nui-game-internal");
        private static readonly byte[] NickKeyAscii = Encoding.ASCII.GetBytes("nickOverride");
        private static readonly byte[] NickKeyUtf16 = Encoding.Unicode.GetBytes("nickOverride");

        public static string GetFiveMCfgPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CitizenFX",
                "fivem.cfg");
        }

        private static string GetDisabledBindsPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "disabled_binds.json");
        }

        private static void AtomicWriteAllText(string path, string content, bool createBackup = true)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (createBackup)
            {
                string bak = path + ".bak";
                if (File.Exists(path) && !File.Exists(bak))
                    File.Copy(path, bak);
            }

            string tmp = path + ".tmp";
            File.WriteAllText(tmp, content, Utf8NoBom);
            File.Move(tmp, path, overwrite: true);
        }

        private static (List<string> Lines, string Eol) ReadLinesPreservingEol(string path)
        {
            var lines = new List<string>();
            string eol = Environment.NewLine;

            if (!File.Exists(path))
                return (lines, eol);

            string orig = File.ReadAllText(path, Encoding.UTF8);
            if (!orig.Contains("\r\n") && orig.Contains('\n'))
                eol = "\n";

            if (!string.IsNullOrEmpty(orig))
            {
                lines.AddRange(orig.Replace("\r\n", "\n").Split('\n'));
                if (lines.Count > 0 && lines[^1] == "")
                    lines.RemoveAt(lines.Count - 1);
            }

            return (lines, eol);
        }

        private static bool HasForbiddenCfgChars(string value)
        {
            foreach (char c in value)
            {
                if (c == '"' || c == '\\' || c == ';' || char.IsControl(c))
                    return true;
            }
            return false;
        }

        public static (string Nickname, bool Found, string? Error) ReadNickname()
        {
            string path = GetFiveMCfgPath();
            if (!File.Exists(path))
                return ("", false, null);

            try
            {
                foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
                {
                    var m = NicknameLineRegex.Match(rawLine);
                    if (m.Success)
                    {
                        string val = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                        return (val, true, null);
                    }
                }
                return ("", false, null);
            }
            catch (Exception ex)
            {
                return ("", false, ex.Message);
            }
        }

        public static (bool Ok, string Message) WriteNickname(string nickname)
        {
            string trimmed = (nickname ?? string.Empty).Trim();
            if (HasForbiddenCfgChars(trimmed))
                return (false, "Nickname tidak boleh mengandung tanda kutip (\"), backslash (\\), titik koma (;), atau karakter kontrol.");

            string path = GetFiveMCfgPath();
            try
            {
                var (lines, eol) = ReadLinesPreservingEol(path);

                if (string.IsNullOrEmpty(trimmed))
                {
                    if (!File.Exists(path))
                        return (true, "Nickname override dihapus.");

                    int removed = lines.RemoveAll(l => NicknameMatchRegex.IsMatch(l));
                    if (removed == 0)
                        return (true, "cl_nickname tidak ditemukan di fivem.cfg.");

                    string next = lines.Count == 0 ? "" : string.Join(eol, lines) + eol;
                    AtomicWriteAllText(path, next);
                    return (true, "cl_nickname dihapus dari fivem.cfg.");
                }

                string newLine = $"seta \"cl_nickname\" \"{trimmed}\"";
                bool replaced = false;
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    if (!NicknameMatchRegex.IsMatch(lines[i])) continue;
                    if (!replaced)
                    {
                        lines[i] = newLine;
                        replaced = true;
                    }
                    else
                    {
                        lines.RemoveAt(i);
                    }
                }

                if (!replaced)
                    lines.Add(newLine);

                AtomicWriteAllText(path, string.Join(eol, lines) + eol);
                return (true, $"Nickname disimpan ke fivem.cfg: {trimmed}");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menyimpan fivem.cfg: {ex.Message}");
            }
        }

        public static (string Value, string? SourceDir, bool Found) ReadNuiNickOverride(string fiveMAppDir)
        {
            if (string.IsNullOrWhiteSpace(fiveMAppDir))
                return ("", null, false);

            string nuiStorage = Path.Combine(fiveMAppDir, "data", "nui-storage");
            if (!Directory.Exists(nuiStorage))
                return ("", null, false);

            var serverDirs = new List<string>();
            try
            {
                foreach (var ctx in Directory.GetDirectories(nuiStorage, "context-server_*"))
                {
                    string lvl = Path.Combine(ctx, "Local Storage", "leveldb");
                    if (Directory.Exists(lvl))
                        serverDirs.Add(lvl);
                }
            }
            catch { }

            var orderedDirs = serverDirs
                .OrderByDescending(GetLevelDbDirWriteTimeUtc)
                .ToList();

            // Also check standard CEF Local Storage locations as fallback
            var fallbackDirs = new List<string>();
            foreach (var rel in new[]
            {
                Path.Combine("Default", "Local Storage", "leveldb"),
                Path.Combine("context-game", "Local Storage", "leveldb"),
                Path.Combine("Local Storage", "leveldb")
            })
            {
                string candidate = Path.Combine(nuiStorage, rel);
                if (Directory.Exists(candidate))
                    fallbackDirs.Add(candidate);
            }
            orderedDirs.AddRange(fallbackDirs.OrderByDescending(GetLevelDbDirWriteTimeUtc));

            foreach (var dir in orderedDirs)
            {
                try
                {
                    var files = Directory.EnumerateFiles(dir)
                        .Where(f => f.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".ldb", StringComparison.OrdinalIgnoreCase))
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(fi => fi.LastWriteTimeUtc)
                        .ThenBy(fi => fi.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                        .ToList();

                    foreach (var fi in files)
                    {
                        try
                        {
                            byte[] data = ReadSharedBytes(fi.FullName);
                            if (data.Length == 0) continue;

                            string? nick = fi.Extension.Equals(".ldb", StringComparison.OrdinalIgnoreCase)
                                ? TryExtractNickFromLdb(data) ?? TryExtractNickFromRawBytes(data)
                                : TryExtractNickFromRawBytes(data);

                            if (!string.IsNullOrEmpty(nick))
                                return (nick, dir, true);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return ("", null, false);
        }

        private static DateTime GetLevelDbDirWriteTimeUtc(string levelDbDir)
        {
            DateTime latest = Directory.GetLastWriteTimeUtc(levelDbDir);
            try
            {
                foreach (var f in Directory.EnumerateFiles(levelDbDir))
                {
                    if (!f.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
                        && !f.EndsWith(".ldb", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var wt = File.GetLastWriteTimeUtc(f);
                    if (wt > latest) latest = wt;
                }
            }
            catch { }
            return latest;
        }

        private static byte[] ReadSharedBytes(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (fs.Length == 0 || fs.Length > 32 * 1024 * 1024)
                return Array.Empty<byte>();

            byte[] buf = new byte[fs.Length];
            int total = 0;
            while (total < buf.Length)
            {
                int read = fs.Read(buf, total, buf.Length - total);
                if (read == 0) break;
                total += read;
            }
            return total == buf.Length ? buf : buf[..total];
        }

        private static string? TryExtractNickFromLdb(byte[] sst)
        {
            if (sst.Length < 48 || !sst.AsSpan(sst.Length - 8, 8).SequenceEqual(SstMagic))
                return null;

            int footerPos = sst.Length - 48;
            if (!TryReadVarint64(sst, ref footerPos, out _) ||
                !TryReadVarint64(sst, ref footerPos, out _) ||
                !TryReadVarint64(sst, ref footerPos, out ulong indexOffset) ||
                !TryReadVarint64(sst, ref footerPos, out ulong indexSize))
                return null;

            byte[]? indexBlock = ReadSstBlock(sst, indexOffset, indexSize);
            if (indexBlock == null)
                return null;

            var blockHandles = new List<(ulong Offset, ulong Size)>();
            IterateLevelDbBlock(indexBlock, (key, val) =>
            {
                int vp = 0;
                if (TryReadVarint64(val, ref vp, out ulong off) && TryReadVarint64(val, ref vp, out ulong sz))
                    blockHandles.Add((off, sz));
            });

            ulong bestSeq = 0;
            bool hasBest = false;
            string? bestVal = null;

            foreach (var (off, sz) in blockHandles)
            {
                byte[]? dataBlock = ReadSstBlock(sst, off, sz);
                if (dataBlock == null) continue;

                IterateLevelDbBlock(dataBlock, (fullKey, val) =>
                {
                    if (fullKey.Length < 8) return;
                    ReadOnlySpan<byte> userKey = fullKey[..^8];
                    if (!IsNickOverrideKey(userKey)) return;

                    ulong trailer = BinaryPrimitives.ReadUInt64LittleEndian(fullKey[^8..]);
                    byte valueType = (byte)(trailer & 0xFF);
                    ulong seq = trailer >> 8;

                    if (hasBest && seq < bestSeq) return;
                    hasBest = true;
                    bestSeq = seq;
                    bestVal = valueType == 1 ? DecodeChromiumStorageValue(val) : null;
                });
            }

            return string.IsNullOrEmpty(bestVal) ? null : bestVal;
        }

        private static byte[]? ReadSstBlock(byte[] sst, ulong offset, ulong size)
        {
            if (offset > (ulong)sst.Length || size > (ulong)sst.Length || offset + size + 5 > (ulong)sst.Length)
                return null;

            int off = (int)offset;
            int sz = (int)size;
            byte compressionType = sst[off + sz];
            ReadOnlySpan<byte> raw = sst.AsSpan(off, sz);

            return compressionType switch
            {
                0 => raw.ToArray(),
                1 => DecompressSnappy(raw),
                _ => null
            };
        }

        private static byte[]? DecompressSnappy(ReadOnlySpan<byte> src)
        {
            int pos = 0;
            if (!TryReadVarint64(src, ref pos, out ulong uncompLenLong) || uncompLenLong > 16 * 1024 * 1024)
                return null;

            byte[] dst = new byte[(int)uncompLenLong];
            int outPos = 0;

            while (pos < src.Length)
            {
                byte tag = src[pos++];
                int type = tag & 0x03;
                if (type == 0)
                {
                    int len = (tag >> 2) + 1;
                    if (len > 60)
                    {
                        int extraBytes = len - 60;
                        if (extraBytes > 4 || extraBytes > src.Length - pos) return null;
                        uint ext = 0;
                        for (int i = 0; i < extraBytes; i++)
                            ext |= (uint)src[pos++] << (8 * i);
                        if (ext >= (uint)int.MaxValue || (long)ext + 1 > src.Length - pos)
                            return null;
                        len = (int)(ext + 1);
                    }
                    if (len <= 0 || len > src.Length - pos || len > dst.Length - outPos) return null;
                    src.Slice(pos, len).CopyTo(dst.AsSpan(outPos, len));
                    pos += len;
                    outPos += len;
                }
                else
                {
                    int len;
                    int offset;
                    if (type == 1)
                    {
                        if (pos >= src.Length) return null;
                        len = ((tag >> 2) & 0x07) + 4;
                        offset = ((tag & 0xE0) << 3) | src[pos++];
                    }
                    else if (type == 2)
                    {
                        if (pos + 2 > src.Length) return null;
                        len = (tag >> 2) + 1;
                        offset = BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(pos, 2));
                        pos += 2;
                    }
                    else
                    {
                        if (pos + 4 > src.Length) return null;
                        len = (tag >> 2) + 1;
                        uint off32 = BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(pos, 4));
                        if (off32 > int.MaxValue) return null;
                        offset = (int)off32;
                        pos += 4;
                    }

                    if (offset <= 0 || offset > outPos || outPos + len > dst.Length) return null;
                    for (int k = 0; k < len; k++)
                    {
                        dst[outPos] = dst[outPos - offset];
                        outPos++;
                    }
                }
            }

            return outPos == dst.Length ? dst : null;
        }

        private delegate void BlockEntryVisitor(ReadOnlySpan<byte> key, ReadOnlySpan<byte> value);

        private static void IterateLevelDbBlock(ReadOnlySpan<byte> block, BlockEntryVisitor visitor)
        {
            if (block.Length < 4) return;
            uint numRestarts = BinaryPrimitives.ReadUInt32LittleEndian(block[^4..]);
            long restartsBytes = (long)numRestarts * 4 + 4;
            if (restartsBytes > block.Length) return;

            int limit = block.Length - (int)restartsBytes;
            int pos = 0;
            byte[] prevKey = Array.Empty<byte>();

            while (pos < limit)
            {
                if (!TryReadVarint32(block, ref pos, out int shared) ||
                    !TryReadVarint32(block, ref pos, out int unshared) ||
                    !TryReadVarint32(block, ref pos, out int valLen))
                    break;

                if (shared < 0 || shared > prevKey.Length || unshared < 0 || valLen < 0)
                    break;
                if (pos + unshared + valLen > limit)
                    break;

                byte[] fullKey = new byte[shared + unshared];
                if (shared > 0)
                    Array.Copy(prevKey, 0, fullKey, 0, shared);
                block.Slice(pos, unshared).CopyTo(fullKey.AsSpan(shared, unshared));
                pos += unshared;

                ReadOnlySpan<byte> valSpan = block.Slice(pos, valLen);
                pos += valLen;

                visitor(fullKey, valSpan);
                prevKey = fullKey;
            }
        }

        private static bool IsNickOverrideKey(ReadOnlySpan<byte> userKey)
        {
            int prefixLen = NuiOriginBytes.Length + 2;
            if (userKey.Length < prefixLen) return false;
            if (!userKey[..NuiOriginBytes.Length].SequenceEqual(NuiOriginBytes)) return false;
            if (userKey[NuiOriginBytes.Length] != 0x00) return false;

            byte flag = userKey[NuiOriginBytes.Length + 1];
            ReadOnlySpan<byte> rest = userKey[prefixLen..];
            if (flag == 0x01)
                return rest.SequenceEqual(NickKeyAscii);
            if (flag == 0x00)
                return rest.SequenceEqual(NickKeyUtf16);
            return false;
        }

        private static string? TryExtractNickFromRawBytes(byte[] data)
        {
            ReadOnlySpan<byte> span = data;
            string? latest = null;

            for (int i = 0; i <= span.Length - (NuiOriginBytes.Length + 2 + NickKeyAscii.Length); i++)
            {
                if (!span.Slice(i, NuiOriginBytes.Length).SequenceEqual(NuiOriginBytes))
                    continue;

                int sepPos = i + NuiOriginBytes.Length;
                if (span[sepPos] != 0x00) continue;

                byte flag = span[sepPos + 1];
                int keyStart = sepPos + 2;
                int keyEnd = -1;

                if (flag == 0x01 && keyStart + NickKeyAscii.Length <= span.Length &&
                    span.Slice(keyStart, NickKeyAscii.Length).SequenceEqual(NickKeyAscii))
                {
                    keyEnd = keyStart + NickKeyAscii.Length;
                }
                else if (flag == 0x00 && keyStart + NickKeyUtf16.Length <= span.Length &&
                         span.Slice(keyStart, NickKeyUtf16.Length).SequenceEqual(NickKeyUtf16))
                {
                    keyEnd = keyStart + NickKeyUtf16.Length;
                }

                if (keyEnd < 0 || keyEnd >= span.Length) continue;

                int vp = keyEnd;
                if (!TryReadVarint32(span, ref vp, out int valLen) || valLen <= 1 || valLen > 512)
                    continue;
                if (vp + valLen > span.Length)
                    continue;

                string? decoded = DecodeChromiumStorageValue(span.Slice(vp, valLen));
                if (!string.IsNullOrEmpty(decoded))
                    latest = decoded;
            }

            return latest;
        }

        private static string? DecodeChromiumStorageValue(ReadOnlySpan<byte> valBytes)
        {
            if (valBytes.Length <= 1) return null;

            byte encodingTag = valBytes[0];
            ReadOnlySpan<byte> payload = valBytes[1..];
            string decoded;

            if (encodingTag == 0x01)
            {
                decoded = Encoding.Latin1.GetString(payload);
            }
            else if (encodingTag == 0x00)
            {
                if (payload.Length % 2 != 0)
                    payload = payload[..^1];
                decoded = Encoding.Unicode.GetString(payload);
            }
            else
            {
                return null;
            }

            decoded = decoded.TrimEnd('\0').Trim();
            if (string.IsNullOrEmpty(decoded)) return null;
            foreach (char c in decoded)
            {
                if (char.IsControl(c)) return null;
            }
            return decoded;
        }

        private static bool TryReadVarint32(ReadOnlySpan<byte> buf, ref int pos, out int value)
        {
            if (TryReadVarint64(buf, ref pos, out ulong v64) && v64 <= int.MaxValue)
            {
                value = (int)v64;
                return true;
            }
            value = 0;
            return false;
        }

        private static bool TryReadVarint64(ReadOnlySpan<byte> buf, ref int pos, out ulong value)
        {
            value = 0;
            int shift = 0;
            while (pos < buf.Length && shift <= 63)
            {
                byte b = buf[pos++];
                value |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return true;
                shift += 7;
            }
            value = 0;
            return false;
        }

        private static bool TryReadQuotedString(string line, ref int pos, out string value)
        {
            value = string.Empty;
            if (pos >= line.Length || line[pos] != '"')
                return false;

            pos++;
            var sb = new StringBuilder();
            bool closed = false;
            while (pos < line.Length)
            {
                if (line[pos] == '\\' && pos + 1 < line.Length && line[pos + 1] == '"')
                {
                    sb.Append('"');
                    pos += 2;
                }
                else if (line[pos] == '"')
                {
                    closed = true;
                    pos++;
                    break;
                }
                else
                {
                    sb.Append(line[pos++]);
                }
            }

            if (!closed)
                return false;

            value = sb.ToString().Trim();
            return !string.IsNullOrEmpty(value);
        }

        private static bool TryReadBindToken(string line, ref int pos, out string token)
        {
            token = string.Empty;
            while (pos < line.Length && char.IsWhiteSpace(line[pos]))
                pos++;

            if (pos >= line.Length)
                return false;
            if (pos + 1 < line.Length && line[pos] == '/' && line[pos + 1] == '/')
                return false;

            if (line[pos] == '"')
                return TryReadQuotedString(line, ref pos, out token);

            int start = pos;
            while (pos < line.Length && !char.IsWhiteSpace(line[pos]))
            {
                if (pos + 1 < line.Length && line[pos] == '/' && line[pos + 1] == '/')
                    break;
                pos++;
            }

            token = line[start..pos].Trim();
            return !string.IsNullOrEmpty(token);
        }

        public static bool ParseBindLine(string rawLine, out string mapper, out string key, out string command)
        {
            mapper = string.Empty;
            key = string.Empty;
            command = string.Empty;

            if (string.IsNullOrWhiteSpace(rawLine))
                return false;

            int pos = 0;
            while (pos < rawLine.Length && char.IsWhiteSpace(rawLine[pos]))
                pos++;

            if (pos + 4 >= rawLine.Length
                || !rawLine.AsSpan(pos, 4).Equals("bind", StringComparison.OrdinalIgnoreCase)
                || !char.IsWhiteSpace(rawLine[pos + 4]))
            {
                return false;
            }
            pos += 4;

            if (!TryReadBindToken(rawLine, ref pos, out mapper) ||
                !TryReadBindToken(rawLine, ref pos, out key))
            {
                mapper = string.Empty;
                key = string.Empty;
                return false;
            }

            while (pos < rawLine.Length && char.IsWhiteSpace(rawLine[pos]))
                pos++;

            if (pos >= rawLine.Length || (pos + 1 < rawLine.Length && rawLine[pos] == '/' && rawLine[pos + 1] == '/'))
                return false;

            if (rawLine[pos] == '"')
                return TryReadQuotedString(rawLine, ref pos, out command);

            int end = rawLine.Length;
            bool inQuotes = false;
            for (int i = pos; i < rawLine.Length; i++)
            {
                if (inQuotes)
                {
                    if (rawLine[i] == '\\' && i + 1 < rawLine.Length && rawLine[i + 1] == '"')
                        i++;
                    else if (rawLine[i] == '"')
                        inQuotes = false;
                }
                else
                {
                    if (rawLine[i] == '"')
                        inQuotes = true;
                    else if (i + 1 < rawLine.Length && rawLine[i] == '/' && rawLine[i + 1] == '/')
                    {
                        end = i;
                        break;
                    }
                }
            }

            command = rawLine[pos..end].Trim().Replace("\\\"", "\"");
            return !string.IsNullOrEmpty(command);
        }

        public static (List<FiveMKeybind> Items, string? Error) ReadKeybinds()
        {
            var result = new List<FiveMKeybind>();
            var seen = new HashSet<(string Mapper, string Key)>();
            string? err = null;

            string cfgPath = GetFiveMCfgPath();
            if (File.Exists(cfgPath))
            {
                try
                {
                    foreach (var rawLine in File.ReadLines(cfgPath, Encoding.UTF8))
                    {
                        if (!ParseBindLine(rawLine, out string mapper, out string key, out string cmd))
                            continue;

                        var id = (mapper.ToUpperInvariant(), key.ToUpperInvariant());
                        if (!seen.Add(id)) continue;

                        result.Add(new FiveMKeybind
                        {
                            Mapper = mapper,
                            Key = key,
                            Command = cmd,
                            Disabled = false
                        });
                    }
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                }
            }

            string disabledPath = GetDisabledBindsPath();
            if (File.Exists(disabledPath))
            {
                try
                {
                    string json = File.ReadAllText(disabledPath, Encoding.UTF8);
                    var disabled = JsonSerializer.Deserialize<List<FiveMKeybind>>(json);
                    if (disabled != null)
                    {
                        foreach (var item in disabled)
                        {
                            string mapper = (item.Mapper ?? "").Trim();
                            string key = (item.Key ?? "").Trim();
                            string cmd = (item.Command ?? "").Trim();
                            if (string.IsNullOrEmpty(mapper) || string.IsNullOrEmpty(key)) continue;

                            var id = (mapper.ToUpperInvariant(), key.ToUpperInvariant());
                            if (!seen.Add(id)) continue;

                            result.Add(new FiveMKeybind
                            {
                                Mapper = mapper,
                                Key = key,
                                Command = cmd,
                                Disabled = true
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    err ??= ex.Message;
                }
            }

            return (result, err);
        }

        public static (bool Ok, string Message) SaveKeybinds(List<FiveMKeybind> items)
        {
            if (items == null)
                return (false, "Daftar keybind tidak valid.");

            var activeBinds = new List<FiveMKeybind>();
            var disabledBinds = new List<FiveMKeybind>();
            var activeSeen = new HashSet<(string Mapper, string Key)>();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                string mapper = (item.Mapper ?? "").Trim();
                string key = (item.Key ?? "").Trim();
                string cmd = (item.Command ?? "").Trim();

                if (!TokenRegex.IsMatch(mapper))
                    return (false, $"Baris {i + 1}: Mapper '{mapper}' tidak valid (gunakan A-Z, 0-9, _ maksimal 32 karakter).");
                if (!TokenRegex.IsMatch(key))
                    return (false, $"Baris {i + 1}: Key '{key}' tidak valid (gunakan A-Z, 0-9, _ maksimal 32 karakter).");
                if (string.IsNullOrEmpty(cmd) || cmd.Length > 200)
                    return (false, $"Baris {i + 1}: Command harus 1–200 karakter.");
                if (HasForbiddenCfgChars(cmd))
                    return (false, $"Baris {i + 1}: Command tidak boleh mengandung tanda kutip (\"), backslash (\\), titik koma (;), atau karakter kontrol.");

                var clean = new FiveMKeybind
                {
                    Mapper = mapper,
                    Key = key,
                    Command = cmd,
                    Disabled = item.Disabled
                };

                if (clean.Disabled)
                {
                    disabledBinds.Add(clean);
                }
                else
                {
                    var id = (mapper.ToUpperInvariant(), key.ToUpperInvariant());
                    if (!activeSeen.Add(id))
                        return (false, $"Duplikat keybind aktif untuk ({mapper}, {key}).");
                    activeBinds.Add(clean);
                }
            }

            try
            {
                string cfgPath = GetFiveMCfgPath();
                var (lines, eol) = ReadLinesPreservingEol(cfgPath);

                // Remove only active `bind ...` lines; preserve `rbind`, `unbindall`, `seta`, comments, etc.
                lines.RemoveAll(l => AnyBindPrefixRegex.IsMatch(l));

                foreach (var b in activeBinds)
                    lines.Add($"bind {b.Mapper} {b.Key} \"{b.Command}\"");

                string cfgContent = lines.Count == 0 ? "" : string.Join(eol, lines) + eol;
                AtomicWriteAllText(cfgPath, cfgContent, createBackup: true);

                string disabledPath = GetDisabledBindsPath();
                string json = JsonSerializer.Serialize(disabledBinds, new JsonSerializerOptions { WriteIndented = true });
                AtomicWriteAllText(disabledPath, json, createBackup: false);

                return (true, $"Keybind disimpan ({activeBinds.Count} aktif, {disabledBinds.Count} nonaktif).");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menyimpan keybind: {ex.Message}");
            }
        }
    }
}
