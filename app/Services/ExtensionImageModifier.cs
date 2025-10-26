using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeaIce.Services;

internal class ExtensionImageModifier : IImageModifier
{
    public static readonly System.Drawing.PointF[] ICE_AREA =
    [
        new System.Drawing.PointF(100f/1461, 150f/1740),
        new System.Drawing.PointF(1070f/1461, 150f/1740),
        new System.Drawing.PointF(1070f/1461, 1612f/1740),
        new System.Drawing.PointF(100f/1461, 1612f/1740),
    ];

    public string Name { get; }
    public BitmapSource Bitmap => _bitmap;

    public ExtensionImageModifier(string filename, Color? iceColor = null)
    {
        _filename = filename;
        _bitmap = BitmapFromUri(new Uri(filename));

        Name = IceExtension.GetFriendlyImageName(filename);

        if (iceColor != null)
        {
            _bitmap = Colorize(_bitmap, iceColor.Value);
        }
    }

    public void DeleteFile()
    {
        File.Delete(_filename);
    }

    public static BitmapSource Colorize(BitmapSource source, Color color)
    {
        if (source.Format != PixelFormats.Bgr32 && source.Format != PixelFormats.Bgra32)
        {
            throw new Exception($"Image format '{source.Format}' is not handled");
        }

        int width = source.PixelWidth;
        int height = source.PixelHeight;

        var bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
        var stride = width * bytesPerPixel;
        byte[] bytes = new byte[height * stride];
        source.CopyPixels(bytes, stride, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * bytesPerPixel;
                var (b, g, r) = (bytes[offset + 0], bytes[offset + 1], bytes[offset + 2]);

                var relX = (float)x / width;
                var relY = (float)y / height;

                if (Pixel.IsInPolygon(ICE_AREA, relX, relY))
                {
                    if (r != b && b > ICE_COLOR_THRESHOLD)
                    {
                        bytes[offset + 0] = color.B;
                        bytes[offset + 1] = color.G;
                        bytes[offset + 2] = color.R;
                    }
                }
            }
        }

        return BitmapSource.Create(width, height, source.DpiX, source.DpiY, PixelFormats.Bgra32, source.Palette, bytes, stride);
    }

    // Internal

    delegate void PixelAction(DrawingContext dc, ref Pixel item, Brush brush);

    const int ICE_COLOR_THRESHOLD = 200;

    readonly string _filename;
    readonly BitmapSource _bitmap;

    private static BitmapImage BitmapFromUri(Uri source)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = source;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        return bitmap;
    }
}
