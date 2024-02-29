namespace CloudStorage.API.Models
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
    }

    public class StorageBlobSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ThumbnailApiKey { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public JwtSettings Jwt { get; set; } = new JwtSettings();
        public StorageBlobSettings BlobStorage { get; set; } = new StorageBlobSettings();
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();
    }
}
