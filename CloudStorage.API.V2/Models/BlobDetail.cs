
using Newtonsoft.Json;

namespace CloudStorage.API.V2.Models {
    public class BlobDetail {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string ThumbnailName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public bool Deleted { get; set; } = false;
    }
}