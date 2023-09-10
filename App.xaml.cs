using System.Windows;

namespace SeaIce;

public partial class App : Application
{
    public App() : base()
    {
        _ = new ThemeController();
    }
}
