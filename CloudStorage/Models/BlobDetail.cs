using CloudStorage.Interfaces;
using Newtonsoft.Json;

namespace CloudStorage.Models
{
    public class BlobDetail : IBlobDetail
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "containerName")]
        public string ContainerName { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "blobname")]
        public string BlobName { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "fileExtension")]
        public string FileExtension { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "deleted")]
        public bool Deleted { get; set; } = false;

        [JsonProperty(PropertyName = "thumbnail")]
        public string Thumbnail { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "thumbnailUrl")]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "private")]
        public bool Private { get; set; } = false;

        [JsonProperty(PropertyName = "created")]
        public DateTime? Created { get; set; }
    }
}
