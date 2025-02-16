using JB.Email.Interfaces;
namespace CloudStorage.API.Models
{
    public class EmailBody : IEmailBody
    {
        public string Content { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }
}
