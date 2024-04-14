using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CloudStorage.Interfaces;
using CloudStorage.WebApp.Services;

namespace CloudStorage.WebApp.Pages
{
    public class GalleryModel : PageModel
    {
        IBlobService _blobService;
        public IEnumerable<IBlobDetail>? BlobDetails { get; protected set; } = null;

        public GalleryModel(IBlobService blobService)
        {
           _blobService = blobService;
        }

        public async void OnGet()
        {
            BlobDetails = await _blobService.GetBlobDetailsAsync();
        }
    }
}
