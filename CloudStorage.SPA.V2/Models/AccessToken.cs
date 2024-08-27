namespace CloudStorage.SPA.V2.Models
{
    public class AccessToken
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public User? User { get; set; }
    }
}
