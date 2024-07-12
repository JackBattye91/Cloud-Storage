namespace CloudStorage.API.V2.Models
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int ExpiresAfterSeconds { get; set; } = 3600;
    }

    public class AppSettings
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
    }
}
