using CloudStorage.Interfaces;
using Newtonsoft.Json;
using System.ComponentModel;

namespace CloudStorage.Models
{
    public class Token : IToken
    {
        public string TokenId { get; set; } = string.Empty;
        public string WebToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }
}
