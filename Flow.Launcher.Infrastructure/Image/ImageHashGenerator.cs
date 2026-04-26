using System;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Infrastructure.Image
{
    public interface IImageHashGenerator
    {
        string GetHashFromImage(ImageSource image);
    }

    public class ImageHashGenerator : IImageHashGenerator
    {
        public string GetHashFromImage(ImageSource imageSource)
        {
            if (imageSource is not BitmapSource { IsFrozen: true } image)
            {
                return null;
            }

            try
            {
                // Normalize to a deterministic pixel format (32-bit premultiplied BGRA)
                BitmapSource normalized = image;
                if (normalized.Format != PixelFormats.Pbgra32)
                {
                    var converted = new FormatConvertedBitmap();
                    converted.BeginInit();
                    converted.Source = normalized;
                    converted.DestinationFormat = PixelFormats.Pbgra32;
                    converted.EndInit();
                    converted.Freeze();

                    normalized = converted;
                }

                // Prepare final buffer (should be HashSize x HashSize)
                var bpp = normalized.Format.BitsPerPixel;
                if (bpp < 1)
                    return null;

                var bytesPerPixel = (bpp + 7) / 8;
                var stride = normalized.PixelWidth * bytesPerPixel;
                var pixels = new byte[stride * normalized.PixelHeight];
                normalized.CopyPixels(pixels, stride, 0);

                var hashBytes = SHA1.HashData(pixels);
                return Convert.ToBase64String(hashBytes);
            }
            catch
            {
                return null;
            }

        }
    }
}
