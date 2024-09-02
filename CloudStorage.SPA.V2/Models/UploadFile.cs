namespace CloudStorage.SPA.V2.Models
{
    public class UploadFile
    {
        public BlobDetail BlobDetail { get; set; } = new BlobDetail();
        public Stream File { get; set; } = new MemoryStream();
    }
}
