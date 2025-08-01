using System.Windows.Media;

namespace SeaIce;

public struct Pixel
{
    public int X;
    public int Y;

    public byte Blue;
    public byte Green;
    public byte Red;
    public byte Alpha;

    public bool IsMap;
    public bool IsScale;
    public bool IsLand;
    public bool IsRiver;
    public bool IsSea;
    public double IceThickness;
    public int ScaleDelta;

    public readonly Color Color => Color.FromRgb(Red, Green, Blue);

    /// <summary>
    /// Determines if the given point is inside the polygon
    /// </summary>
    /// <param name="polygon">the vertices of polygon</param>
    /// <param name="x">X of the given point</param>
    /// <param name="y">Y of the given point</param>
    /// <returns>true if the point is inside the polygon; otherwise, false</returns>
    public static bool IsInPolygon(System.Drawing.PointF[] polygon, float x, float y)
    {
        bool result = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            if (polygon[i].Y < y && polygon[j].Y >= y ||
                polygon[j].Y < y && polygon[i].Y >= y)
            {
                if (polygon[i].X + (y - polygon[i].Y) /
                   (polygon[j].Y - polygon[i].Y) *
                   (polygon[j].X - polygon[i].X) < x)
                {
                    result = !result;
                }
            }
            j = i;
        }
        return result;
    }
}
