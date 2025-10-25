using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeaIce.Services;

internal class ImageCombiner
{
    public static BitmapSource Combine(BitmapSource image1, BitmapSource image2, string name1, string name2)
    {
        var bmp1 = ExtensionImageModifier.Colorize(image1, Color.FromRgb(R1, G1, B1));
        var bmp2 = ExtensionImageModifier.Colorize(image2, Color.FromRgb(R2, G2, B2));

        var bmp = Merge(bmp1, bmp2);
        return Print(bmp, name1, name2);
    }

    // Internal

    const byte R1 = 128, G1 = 192, B1 = 255;
    const byte R2 = 128, G2 = 255, B2 = 192;
    const byte R3 = 255, G3 = 255, B3 = 255;

    private static BitmapSource Merge(BitmapSource source1, BitmapSource source2)
    {
        if ((source1.Format != PixelFormats.Bgr32 && source1.Format != PixelFormats.Bgra32) ||
            source1.Format.BitsPerPixel != source2.Format.BitsPerPixel ||
            source1.Width * source1.Height != source2.Width * source2.Height)
        {
            throw new Exception($"Wrong image format or size");
        }

        int width = source1.PixelWidth;
        int height = source1.PixelHeight;

        var bytesPerPixel = (source1.Format.BitsPerPixel + 7) / 8;
        var stride = width * bytesPerPixel;

        byte[] result = new byte[height * stride];
        byte[] bytes1 = new byte[height * stride];
        byte[] bytes2 = new byte[height * stride];

        source1.CopyPixels(bytes1, stride, 0);
        source2.CopyPixels(bytes2, stride, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * bytesPerPixel;
                var (b1, g1, r1) = (bytes1[offset + 0], bytes1[offset + 1], bytes1[offset + 2]);
                var (b2, g2, r2) = (bytes2[offset + 0], bytes2[offset + 1], bytes2[offset + 2]);

                bool isIce1 = r1 == R1 && g1 == G1 && b1 == B1;
                bool isIce2 = r2 == R2 && g2 == G2 && b2 == B2;

                if (y < 100)
                {
                    result[offset + 0] = 79;
                    result[offset + 1] = 79;
                    result[offset + 2] = 79;
                }
                else if (isIce1 && isIce2)
                {
                    result[offset + 0] = B3;
                    result[offset + 1] = G3;
                    result[offset + 2] = R3;
                }
                else if (isIce1)
                {
                    result[offset + 0] = B1;
                    result[offset + 1] = G1;
                    result[offset + 2] = R1;
                }
                else if (isIce2)
                {
                    result[offset + 0] = B2;
                    result[offset + 1] = G2;
                    result[offset + 2] = R2;
                }
                else
                {
                    result[offset + 0] = Math.Max(b1, b2);
                    result[offset + 1] = Math.Max(g1, g2);
                    result[offset + 2] = Math.Max(r1, r2);
                }

                if (bytesPerPixel > 3)
                    result[offset + 3] = 0xff;
            }
        }

        return BitmapSource.Create(width, height, source1.DpiX, source1.DpiY, source1.Format, source1.Palette, result, stride);
    }

    private static RenderTargetBitmap Print(BitmapSource bmp, string name1, string name2)
    {
        var dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            var rect = new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight);
            dc.DrawImage(bmp, rect);

            var lbl1 = new FormattedText(name1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Arial"), 60, new SolidColorBrush(Color.FromRgb(R1, G1, B1)));
            dc.DrawText(lbl1, new Point(60, 30));

            var lbl2 = new FormattedText(name2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Arial"), 60, new SolidColorBrush(Color.FromRgb(R2, G2, B2)));
            dc.DrawText(lbl2, new Point(700, 30));
            dc.Close();
        }

        var rtb = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);

        return rtb;
    }
}
