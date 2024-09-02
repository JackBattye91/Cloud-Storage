namespace CloudStorage.API.V2.Models
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public int ExpiresAfterSeconds { get; set; } = 3600;
    }

    public class DatabaseSettings
    {
        public string Database { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class BlobSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();
        public BlobSettings Blob { get; set; } = new BlobSettings();
        public EmailSettings Email { get; set; } = new EmailSettings();
    }
}
