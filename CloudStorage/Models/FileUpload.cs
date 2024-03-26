namespace CloudStorage.Models
{
    public class FileUpload
    {
        public string FileName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string DataBase64 { get; set; } = string.Empty;
        public byte[]? Data { get; set; }
        public Stream? DataStream { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public bool IsPrivate { get; set; } = false;
    }
}
