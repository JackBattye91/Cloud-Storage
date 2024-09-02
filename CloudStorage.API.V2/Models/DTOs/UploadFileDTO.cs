namespace CloudStorage.API.V2.Models.DTOs
{
    public class UploadFileDTO
    {
        public BlobDetail BlobDetail { get; set; } = new BlobDetail();
        public Stream File { get; set; } = new MemoryStream();
    }
}
