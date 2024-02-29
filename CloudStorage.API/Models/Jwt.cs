using Newtonsoft.Json;

namespace CloudStorage.API.Models
{
    public class JwtHeader
    {
        [JsonProperty(PropertyName = "alg")]
        public string Algorithm { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "typ")]
        public string TokenType { get; set; } = string.Empty;
    }

    public class JwtPayload
    {
        [JsonProperty(PropertyName = "nbf")]
        public string NotBefore { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "exp")]
        public string Expires { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "iat")]
        public string IssedAt { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "iss")]
        public string Issuer { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "aud")]
        public string Audience { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "sub")]
        public string Subject { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "per")]
        public string Permissions = string.Empty;
    }
    public class Jwt
    {
        public JwtHeader Header { get; set; } = new JwtHeader();
        public JwtPayload Payload { get; set; } = new JwtPayload();
    }
}
