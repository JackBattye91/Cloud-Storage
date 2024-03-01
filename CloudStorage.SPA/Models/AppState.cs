using CloudStorage.Interfaces;
using CloudStorage.Models;

namespace CloudStorage.SPA.Models
{
    public class AppState
    {
        public IUser? CurrentUser { get; set; } = null;
        public IList<BlobDetail>? BlobDetails { get; set; } = null;
    }
}
