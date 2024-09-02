using Newtonsoft.Json;
namespace CloudStorage.API.V2.Models
{
    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string Forenames { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;

        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool Activated { get; set; } = false;
    }
}
