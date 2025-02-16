namespace CloudStorage.API.Models
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
        public string ConnectionString { get; set; } = string.Empty;
        public string PasswordPepper { get; set; } = string.Empty;
        public string PasswordResetKey { get; set; } = string.Empty;
    }

    public class StorageBlobSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ThumbnailApiKey { get; set; } = string.Empty;
    }

    public class EmailSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
        public StorageBlobSettings BlobStorage { get; set; } = new StorageBlobSettings();
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();
        public EmailSettings Email { get; set; } = new EmailSettings();

        public string WebAppUrl { get; set; } = string.Empty;
    }
}
