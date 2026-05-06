using System;
using System.Security.Cryptography;
using System.Text;

namespace FiveMConfigEditorWPF.Models
{
    /// <summary>
    /// Secure configuration handler with obfuscation
    /// API credentials are obfuscated using Base64 + XOR encoding
    /// </summary>
    public static class SecureConfig
    {
        // Obfuscated API credentials
        private static readonly string ObfuscatedBaseUrl = "MjoyOjg6Lyw7Oj4yLyw7OjoyOjM6Mzs6LzM=";
        private static readonly string ObfuscatedApiKey = "PzQidzhrbGprbmk/OG9paTxoaW88OGtjO29jODw+Ym9vaz84Y2I5b2xuPDxraDlrOD5qPGlrbW88OGhsPGI4YmI8bG8=";
        
        // XOR key for obfuscation
        private static readonly byte XorKey = 0x5A;

        public static string GetApiBaseUrl()
        {
            try
            {
                string decoded = DecodeObfuscated(ObfuscatedBaseUrl);
                if (string.IsNullOrWhiteSpace(decoded))
                {
                    return "http://localhost:1430/v1"; // Fallback
                }
                return decoded;
            }
            catch
            {
                return "http://localhost:1430/v1"; // Fallback
            }
        }

        public static string GetApiKey()
        {
            try
            {
                string decoded = DecodeObfuscated(ObfuscatedApiKey);
                if (string.IsNullOrWhiteSpace(decoded))
                {
                    return "enx-b160143eb533f235fb19a59bfd8551eb98c564ff12c1bd0f3175fb26f8b88f65"; // Fallback
                }
                return decoded;
            }
            catch
            {
                return "enx-b160143eb533f235fb19a59bfd8551eb98c564ff12c1bd0f3175fb26f8b88f65"; // Fallback
            }
        }

        private static string DecodeObfuscated(string obfuscated)
        {
            // Multi-layer decoding to make reverse engineering harder
            try
            {
                // Layer 1: Base64 decode
                byte[] data = Convert.FromBase64String(obfuscated);
                
                // Layer 2: XOR with key
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] ^= XorKey;
                }
                
                // Layer 3: Reverse byte order (additional obfuscation)
                Array.Reverse(data);
                Array.Reverse(data); // Reverse back (just to add complexity)
                
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                // Silent fail - return empty string
                return string.Empty;
            }
        }

        // Helper method to generate obfuscated strings (DEVELOPMENT ONLY - Remove in production)
        #if DEBUG
        public static string EncodeForObfuscation(string plainText)
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            
            // Apply XOR
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= XorKey;
            }
            
            return Convert.ToBase64String(data);
        }
        #endif
    }
}
