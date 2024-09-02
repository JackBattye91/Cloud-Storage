﻿using Newtonsoft.Json;

namespace CloudStorage.API.V2.Models.DTOs
{
    public class CreateUserDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Forenames { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }
}
