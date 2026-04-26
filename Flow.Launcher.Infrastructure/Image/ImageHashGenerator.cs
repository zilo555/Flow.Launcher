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
        private const int HashSize = 64; // Deterministic size for hashing to avoid DPI/stride variability

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

                // Draw into a fixed-size RenderTarget at 96 DPI to ensure deterministic pixel buffer
                var rtb = new RenderTargetBitmap(HashSize, HashSize, 96, 96, PixelFormats.Pbgra32);
                var dv = new DrawingVisual();

                using (var dc = dv.RenderOpen())
                {
                    // Uniformly scale to fit into HashSize x HashSize
                    var scale = Math.Min((double)HashSize / normalized.PixelWidth, (double)HashSize / normalized.PixelHeight);
                    var drawWidth = normalized.PixelWidth * scale;
                    var drawHeight = normalized.PixelHeight * scale;

                    var offsetX = (HashSize - drawWidth) / 2;
                    var offsetY = (HashSize - drawHeight) / 2;
                    dc.DrawImage(normalized, new System.Windows.Rect(offsetX, offsetY, drawWidth, drawHeight));
                }

                rtb.Render(dv);
                rtb.Freeze();

                // Extract raw pixel bytes
                var bpp = rtb.Format.BitsPerPixel;
                if (bpp < 1)
                    return null;

                var bytesPerPixel = (bpp + 7) / 8;
                var stride = rtb.PixelWidth * bytesPerPixel;

                var bufferSize = stride * rtb.PixelHeight;
                if (bufferSize < 1)
                    return null;

                var pixels = new byte[bufferSize];
                rtb.CopyPixels(pixels, stride, 0);

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
