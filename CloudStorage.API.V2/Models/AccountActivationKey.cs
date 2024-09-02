using Newtonsoft.Json;
using System.Data;

namespace CloudStorage.API.V2.Models
{
    public class AccountActivationKey
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ActivationKey { get; set; } = string.Empty;
        public DateTime Expires { get; set; } = DateTime.MinValue;
    }
}
