using System;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;

namespace ImageConverter
{
    public class ImageConversionService
    {
        public static async Task<MemoryStream> Convert(Stream inputImage)
        {
            ArgumentNullException.ThrowIfNull(inputImage);

            using var source = new MagickImage(inputImage);

            var result = new MemoryStream();
            await source.WriteAsync(result, MagickFormat.Jpg);

            result.Seek(0, SeekOrigin.Begin);

            return result;
        }
    }
}