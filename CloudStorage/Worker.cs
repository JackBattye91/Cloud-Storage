using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudStorage
{
    public static class Worker
    {
        public static async Task<Stream> CopyTo(Stream pBaseStream)
        {
            MemoryStream memoryStream = new MemoryStream();
            byte[] bytes = new byte[512];
            int bytesRead = 0;

            do
            {
                bytesRead = await pBaseStream.ReadAsync(bytes, 0, 512);
                memoryStream.Write(bytes, 0, bytesRead);
            } while (bytesRead > 0);

            return memoryStream;
        }
        public static string CreateBase64Thumbnail(string pImageBase64, int pMaxWidth, int pMaxHeight)
        {
            return Convert.ToBase64String(CreateThumbnail(pImageBase64, pMaxWidth, pMaxHeight));
        }

        public static byte[] CreateThumbnail(string pImageBase64, int pMaxWidth, int pMaxHeight)
        {
            byte[] imageData = Convert.FromBase64String(pImageBase64);

            Image baseImage = Image.Load(imageData);

            double ratioX = (double)pMaxWidth / baseImage.Width;
            double ratioY = (double)pMaxHeight / baseImage.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(baseImage.Width * ratio);
            int newHeight = (int)(baseImage.Height * ratio);

            baseImage.Mutate(x => x.Resize(newWidth, newHeight));

            using MemoryStream mStream = new MemoryStream();
            baseImage.Save(mStream, new JpegEncoder());

            return mStream.ToArray();
        }

        public static async Task<byte[]> CreateThumbnail(Stream stream, int pMaxWidth, int pMaxHeight)
        {
            Image baseImage = await Image.LoadAsync(stream);

            double ratioX = (double)pMaxWidth / baseImage.Width;
            double ratioY = (double)pMaxHeight / baseImage.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(baseImage.Width * ratio);
            int newHeight = (int)(baseImage.Height * ratio);

            baseImage.Mutate(x => x.Resize(newWidth, newHeight));

            using MemoryStream mStream = new MemoryStream();
            baseImage.Save(mStream, new JpegEncoder());

            return mStream.ToArray();
        }
    }
}
