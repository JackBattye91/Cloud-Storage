
namespace CloudStorage.API.V2.Models
{
    public class EmailHeader : JB.Email.Interfaces.IEmailHeader
    {
        public string Sender { get; set; } = string.Empty;
        public IEnumerable<string> To { get; set; } = new List<string>();
        public IEnumerable<string> Cc { get; set; } = new List<string>();
        public IEnumerable<string> Bcc { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
    }

    public class EmailBody : JB.Email.Interfaces.IEmailBody
    {
        public string Content { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }
}
