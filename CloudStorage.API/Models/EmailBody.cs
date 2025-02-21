
namespace CloudStorage.API.Models
{
    public class EmailBody
    {
        public string Content { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }
}
