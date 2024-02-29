using CloudStorage.Consts;
using CloudStorage.Interfaces;
using Newtonsoft.Json;

namespace CloudStorage.Models
{
    public class User : IUser
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "passwordSalt")]
        public string PasswordSalt { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "permissions")]
        public Permission Permissions { get; set; } = Permission.None;
    }
}
