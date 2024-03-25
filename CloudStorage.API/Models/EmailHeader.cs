using JB.Email.Interfaces;

namespace CloudStorage.API.Models
{
    public class EmailHeader : IEmailHeader
    {
        public string Sender { get; set; } = string.Empty;
        public IEnumerable<string> To { get; set; } = new List<string>();
        public IEnumerable<string> Cc { get; set; } = new List<string>();
        public IEnumerable<string> Bcc { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
    }
}
