using Newtonsoft.Json;

namespace CloudStorage.API.V2.Models
{
    public class RefreshToken
    {
        [JsonProperty(propertyName: "id")]
        public string Id { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime Expires { get; set; }
    }
}
