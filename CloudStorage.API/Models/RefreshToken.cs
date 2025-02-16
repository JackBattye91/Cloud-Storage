using Newtonsoft.Json;

namespace CloudStorage.API.Models
{
    public class RefreshToken
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "created")]
        public DateTime DateCreated { get; set; } = DateTime.MinValue;
    }
}
