using CloudStorage.Consts;
using Newtonsoft.Json;

namespace CloudStorage.Interfaces
{
    public interface IUser
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; }

        [JsonProperty(PropertyName = "username")]
        string Username { get; set; }

        [JsonProperty(PropertyName = "email")]
        string Email { get; set; }

        [JsonProperty(PropertyName = "password")]
        string Password { get; set; }

        [JsonProperty(PropertyName = "passwordSalt")]
        string PasswordSalt { get; set; }

        [JsonProperty(PropertyName = "permissions")]
        Permission Permissions { get; set; }
    }
}
