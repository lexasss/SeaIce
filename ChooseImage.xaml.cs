using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SeaIce;

public partial class ChooseImage : Window
{
    public ChooseImage()
    {
        InitializeComponent();
    }

    public void SetLinks(string[] links)
    {
        lsvImages.ItemsSource = links.Select(link => new ListViewItem()
        {
            Content = ThinknessImageService.CreateName(link),
            Tag = link
        });
    }

    public string[]? GetSelectedImages()
    {
        List<string> result = new();
        foreach (var image in lsvImages.SelectedItems)
        {
            result.Add((string)((ListViewItem)image).Tag);
        }
        return result.Count > 0 ? result.ToArray() : null;
    }

    // Internal

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Images_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.Source is TextBlock)
        {
            DialogResult = true;
        }
    }
}
