﻿namespace CloudStorage.API.V2.Models.DTOs
{
    public class TokenDTO
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public UserDTO? User { get; set; }
    }
}
