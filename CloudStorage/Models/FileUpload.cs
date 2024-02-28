﻿namespace CloudStorage.Models
{
    public class FileUpload
    {
        public string FileName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string DataBase64 { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
    }
}
