using System.Windows.Media.Imaging;

namespace SeaIce.Services;

internal interface IImageModifier
{
    string Name { get; }
    BitmapSource Bitmap { get; }
    void DeleteFile();
}
