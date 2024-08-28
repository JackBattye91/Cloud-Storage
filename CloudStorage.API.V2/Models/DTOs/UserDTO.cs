using Newtonsoft.Json;

namespace CloudStorage.API.V2.Models.DTOs
{
    public class UserDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Forenames { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }
}
