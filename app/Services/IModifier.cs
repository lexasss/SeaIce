using System.Windows.Media.Imaging;

namespace SeaIce.Services;

internal interface IModifier
{
    string Name { get; }
    BitmapSource Bitmap { get; }
    void DeleteFile();
}
