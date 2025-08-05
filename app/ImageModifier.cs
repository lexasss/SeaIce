using SeaIce.ImageServices;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeaIce;

internal class ImageModifier
{
    [Flags]
    public enum PixelOperation
    {
        None = 0,
        ColorizeLand = 1,
        ColorizeRivers = 2,
        ColorizeIce = 4,
        ColorizeSea = 8,
    }

    public string Name { get; private set; }

    public double IceColorStep { get; set; } = 0.2; // 1 meter

    public ImageModifier(string filename)
    {
        _filename = filename;
        _bitmap = BitmapFromUri(new Uri(filename));
        _size = new Size(_bitmap.PixelWidth, _bitmap.PixelHeight);

        _pixels = ReadPixels(_bitmap);
        _scale = CreateScale(_pixels, _size);

        Name = IceThinkness.GetFriendlyImageName(filename);

        AnnotatePixels(_pixels, _size, _scale);
    }

    /// <summary>
    /// Calculates amount of ice in cubic kilometers
    /// </summary>
    /// <returns></returns>
    public double GetIceAmount()
    {
        double amount = 0;
        int minX = (int)(MAP_RECT_MIN.X * _size.Width);
        int maxX = (int)(MAP_RECT_MAX.X * _size.Width);
        int minY = (int)(MAP_RECT_MIN.Y * _size.Height);
        int maxY = (int)(MAP_RECT_MAX.Y * _size.Height);

        for (int x = minX; x < maxX; x += 1)
        {
            for (int y = minY; y < maxY; y += 1)
            {
                ref var pixel = ref _pixels[x, y];

                if (!pixel.IsMap)
                    continue;

                if (pixel.IceThickness > 0)
                {
                    amount += pixel.IceThickness;
                }
            }
        }

        return amount * SCALE_SIZE / 1000 * MAP_RESOLUTION_PER_PIXEL * MAP_RESOLUTION_PER_PIXEL;
    }

    public RenderTargetBitmap RedrawImage(PixelOperation operations)
    {
        var rect = new Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);
        var dv = new DrawingVisual();

        int minX = (int)(MAP_RECT_MIN.X * _size.Width);
        int maxX = (int)(MAP_RECT_MAX.X * _size.Width);
        int minY = (int)(MAP_RECT_MIN.Y * _size.Height);
        int maxY = (int)(MAP_RECT_MAX.Y * _size.Height);

        var scan = (PixelAction action, DrawingContext dc) =>
        {
            for (int x = minX; x < maxX; x += 1)
            {
                for (int y = minY; y < maxY; y += 1)
                {
                    action(dc, ref _pixels[x, y]);
                }
            }
        };

        using (DrawingContext dc = dv.RenderOpen())
        {
            dc.DrawImage(_bitmap, rect);

            if (operations.HasFlag(PixelOperation.ColorizeLand))
                scan(ColorizeLand, dc);
            if (operations.HasFlag(PixelOperation.ColorizeRivers))
                scan(ColorizeRivers, dc);
            if (operations.HasFlag(PixelOperation.ColorizeSea))
                scan(ColorizeSea, dc);
            if (operations.HasFlag(PixelOperation.ColorizeIce))
                scan(ColorizeIce, dc);

            dc.Close();
        }

        var rtb = new RenderTargetBitmap(_bitmap.PixelWidth, _bitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);

        return rtb;
    }

    public void DeleteFile()
    {
        System.IO.File.Delete(_filename);
    }

    // Internal

    delegate void PixelAction(DrawingContext dc, ref Pixel item);

    static readonly System.Drawing.PointF[] ICE_AREA = new System.Drawing.PointF[]
    {
            new System.Drawing.PointF(0.03f, 0.051113f),
            new System.Drawing.PointF(0.582143f, 0.051113f),
            new System.Drawing.PointF(0.582143f, 0.45342127f),
            new System.Drawing.PointF(0.777143f, 0.45342127f),
            new System.Drawing.PointF(0.777143f, 0.852432f),
            new System.Drawing.PointF(0.03f, 0.852432f),
    };

    static readonly Point MAP_RECT_MIN = new(ICE_AREA[0].X, ICE_AREA[0].Y);
    static readonly Point MAP_RECT_MAX = new(ICE_AREA[^2].X, 1); // ICE_AREA[^2].Y);

    const float SCALE_START_X = 0.0316f;    // rel pixels  23-538,575/728,631    44,1102/1400,1213
    const float SCALE_END_X = 0.74f;        // rel pixels
    const float SCALE_Y = 0.9095238095f;    // rel pixels

    static readonly System.Drawing.PointF[] SCALE_AREA = new System.Drawing.PointF[]
    {
            new System.Drawing.PointF(0.03026f, 0.907936508f),
            new System.Drawing.PointF(0.77304f, 0.907936508f),
            new System.Drawing.PointF(0.77304f, 0.922222222f),
            new System.Drawing.PointF(0.03026f, 0.922222222f),
    };

    const double SCALE_SIZE = 5;                    // meters
    const float MAP_RESOLUTION_PER_PIXEL = 12.5f;   // km

    const int COLOR_THRESHOLD = 200;
    const int ICE_DELTA_THRESHOLD = 50;
    const int SCALE_DELTA_THRESHOLD = 100;

    static readonly Brush LAND_BRUSH = Brushes.ForestGreen;
    static readonly Brush SEA_BRUSH = Brushes.PaleTurquoise;
    static readonly Brush RIVERS_BRUSH = Brushes.DeepSkyBlue;

    static byte K_128_255(double n) => (byte)(n * 127 + 128);
    static byte K_255_128(double n) => (byte)((1.0 - n) * 127 + 128);
    static byte K_0_255(double n) => (byte)(n * 255);
    static byte K_255_0(double n) => (byte)((1.0 - n) * 255);
    static byte K_0_128(double n) => (byte)(n * 128);
    static byte K_128_0(double n) => (byte)((1.0 - n) * 128);
    static byte K_64_255(double n) => (byte)(n * 191 + 64);
    static byte K_255_64(double n) => (byte)((1.0 - n) * 191 + 64);
    static byte K_192_255(double n) => (byte)(n * 63 + 192);
    static byte K_255_192(double n) => (byte)((1.0 - n) * 63 + 192);
    static byte K_0_192(double n) => (byte)(n * 192);
    static byte K_192_0(double n) => (byte)((1.0 - n) * 192);
    static byte K_128_192(double n) => (byte)(n * 64 + 128);
    static byte K_192_128(double n) => (byte)((1.0 - n) * 64 + 128);
    static byte K_64_192(double n) => (byte)(n * 128 + 64);
    static byte K_192_64(double n) => (byte)((1.0 - n) * 128 + 64);
    static byte K_0_64(double n) => (byte)(n * 64);
    static byte K_64_0(double n) => (byte)((1.0 - n) * 64);

    const byte K_0 = 0;
    const byte K_64 = 64;
    const byte K_128 = 128;
    const byte K_192 = 192;
    const byte K_255 = 255;

    readonly string _filename;
    readonly Size _size;
    readonly BitmapImage _bitmap;

    readonly Pixel[,] _pixels;
    readonly Pixel[] _scale;

    private static BitmapImage BitmapFromUri(Uri source)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = source;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        return bitmap;
    }

    private static Pixel[,] ReadPixels(BitmapImage source)
    {
        if (source.Format != PixelFormats.Bgr32)
        {
            throw new Exception($"Image format '{source.Format}' is not handled");
        }

        int width = source.PixelWidth;
        int height = source.PixelHeight;

        var bytesPerPixel = (source.Format.BitsPerPixel + 7) / 8;
        var stride = width * bytesPerPixel;
        byte[] bytes = new byte[height * stride];
        source.CopyPixels(bytes, stride, 0);

        Pixel[,] pixels = new Pixel[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * bytesPerPixel;
                pixels[x, y] = new Pixel
                {
                    X = x,
                    Y = y,
                    Blue = bytes[offset + 0],
                    Green = bytes[offset + 1],
                    Red = bytes[offset + 2],
                    Alpha = 255,
                };
            }
        }

        return pixels;
    }

    private static Pixel[] CreateScale(Pixel[,] pixels, Size size)
    {
        int scaleStartX = (int)(SCALE_START_X * size.Width);
        int scaleEndX = (int)(SCALE_END_X * size.Width);
        int scaleY = (int)(SCALE_Y * size.Height);

        var scale = new Pixel[scaleEndX - scaleStartX];

        for (int x = scaleStartX; x < scaleEndX; x += 1)
        {
            scale[x - scaleStartX] = pixels[x, scaleY];
        }

        return scale;
    }

    private static void AnnotatePixels(Pixel[,] pixels, Size size, Pixel[] scale)
    {
        for (int x = 0; x < size.Width; x += 1)
        {
            for (int y = 0; y < size.Height; y += 1)
            {
                ref var pixel = ref pixels[x, y];
                var relX = (float)(x / size.Width);
                var relY = (float)(y / size.Height);

                if (Pixel.IsInPolygon(ICE_AREA, relX, relY))
                {
                    pixel.IsMap = true;

                    pixel.IsSea = 
                        pixel.Red > COLOR_THRESHOLD && 
                        pixel.Green > COLOR_THRESHOLD && 
                        pixel.Blue > COLOR_THRESHOLD;

                    var (delta, scaleValue) = MapToScale(scale, size, ref pixel);

                    if (delta < ICE_DELTA_THRESHOLD)
                    {
                        pixel.IceThickness = scaleValue;
                        pixel.ScaleDelta = delta;
                    }

                    pixel.IsLand = pixel.IceThickness == 0 && !pixel.IsSea;
                    pixel.IsRiver = 
                        pixel.IsLand && 
                        pixel.Red < COLOR_THRESHOLD && 
                        pixel.Green < COLOR_THRESHOLD && 
                        pixel.Blue > COLOR_THRESHOLD;
                }
                else if (Pixel.IsInPolygon(SCALE_AREA, relX, relY))
                {
                    pixel.IsScale = true;

                    var (delta, scaleValue) = MapToScale(scale, size, ref pixel);
                    if (delta < SCALE_DELTA_THRESHOLD)
                    {
                        pixel.IceThickness = scaleValue;
                        pixel.ScaleDelta = delta;
                    }
                }
            }
        }
    }

    private static (int, double) MapToScale(Pixel[] scale, Size size, ref Pixel pixel)
    {
        var minDelta = int.MaxValue;
        int scaleEndX = (int)(SCALE_END_X * size.Width);
        int scaleStartX = (int)(SCALE_START_X * size.Width);
        double scaleValue = 0;

        for (int x = scaleStartX; x < scaleEndX; x += 1)
        {
            var scalePixel = scale[x - scaleStartX];
            var delta = Math.Abs(pixel.Red - scalePixel.Red) + Math.Abs(pixel.Green - scalePixel.Green) + Math.Abs(pixel.Blue - scalePixel.Blue);
            if (delta < minDelta)
            {
                minDelta = delta;
                scaleValue = (double)x / (scaleEndX - scaleStartX);
            }
        }

        return (minDelta, scaleValue);
    }

    private void ColorizeLand(DrawingContext dc, ref Pixel pixel)
    {
        if (pixel.IsLand)
        {
            dc.DrawRectangle(LAND_BRUSH, null, new Rect(pixel.X, pixel.Y, 1, 1));
        }
    }

    private void ColorizeRivers(DrawingContext dc, ref Pixel pixel)
    {
        if (pixel.IsRiver)
        {
            dc.DrawRectangle(RIVERS_BRUSH, null, new Rect(pixel.X, pixel.Y, 1, 1));
        }
    }

    private void ColorizeSea(DrawingContext dc, ref Pixel pixel)
    {
        if (pixel.IsSea)
        {
            dc.DrawRectangle(SEA_BRUSH, null, new Rect(pixel.X, pixel.Y, 1, 1));
        }
    }

    private void ColorizeIce(DrawingContext dc, ref Pixel pixel)
    {
        if (pixel.IceThickness > 0)
        {
            double meters = pixel.IceThickness;
            double level = Math.Floor(meters / IceColorStep) * IceColorStep;
            double frac = Math.Max(0, meters - level) / IceColorStep;

            int meter = (int)(level / IceColorStep);
            var (r, g, b) = meter switch
            {
                < 1 => (K_255_128(frac), K_255_0(frac),   K_255_192(frac)),
                < 2 => (K_128_0(frac),   K_0_255(frac),   K_192_255(frac)),
                < 3 => (K_0,             K_255_192(frac), K_255_0(frac)),
                < 4 => (K_0_255(frac),   K_192_255(frac), K_0),
                < 5 => (K_255,           K_255_0(frac),   K_0),
                < 6 => (K_255_128(frac), K_0_192(frac),   K_0),
                < 7 => (K_128_255(frac), K_192_0(frac),   K_0_128(frac)),
                < 8 => (K_255_64(frac),  K_0_64(frac),    K_128_0(frac)),
                < 9 => (K_64_0(frac),    K_64_0(frac),    K_0),
                _   => (K_0,             K_0,             K_0),
            };

            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            dc.DrawRectangle(brush, null, new Rect(pixel.X, pixel.Y, 1, 1));
        }
    }
}
