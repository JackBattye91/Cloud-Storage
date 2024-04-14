using CloudStorage.Models;

namespace CloudStorage.WebApp.Models
{
    public class AppSettings
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public Token? AccessToken { get; set; } = null;
    }
}
