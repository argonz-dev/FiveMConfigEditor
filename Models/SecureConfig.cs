namespace FiveMConfigEditorWPF.Models
{
    /// <summary>
    /// Default secure configuration for LLM provider (VPS 103 Authentic)
    /// </summary>
    public static class SecureConfig
    {
        public static string GetApiBaseUrl()
        {
            return "https://103.103.22.86:20128/v1";
        }

        public static string GetApiKey()
        {
            return "tuxevil";
        }

        public static string GetDefaultModel()
        {
            return "gemini-3.8-flash-high";
        }
    }
}
