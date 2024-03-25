using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage.Interfaces
{
    public interface IBlobDetail
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; }

        [JsonProperty(PropertyName = "fileName")]
        string FileName { get; set; }

        [JsonProperty(PropertyName = "containerName")]
        string ContainerName { get; set; }

        [JsonProperty(PropertyName = "userId")]
        string UserId { get; set; }

        [JsonProperty(PropertyName = "blobname")]
        string BlobName { get; set; }

        [JsonProperty(PropertyName = "fileExtension")]
        string FileExtension { get; set; }

        [JsonProperty(PropertyName = "deleted")]
        bool Deleted { get; set; }

        [JsonProperty(PropertyName = "thumbnail")]
        string Thumbnail { get; set; }

        [JsonProperty(PropertyName = "thumbnailUrl")]
        string ThumbnailUrl { get; set; }

        [JsonProperty(PropertyName = "private")]
        bool Private { get; set; }

        [JsonProperty(PropertyName = "created")]
        DateTime? Created { get; set; }
    }
}
