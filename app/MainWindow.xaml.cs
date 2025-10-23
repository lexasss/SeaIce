using ModernWpf;
using SeaIce.ImageServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SeaIce;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public bool IsThemeLight
    { 
        get => ThemeManager.Current.ApplicationTheme == ApplicationTheme.Light;
        set
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsThemeLight)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsThemeDark)));
        }
    }
    public bool IsThemeDark
    {
        get => ThemeManager.Current.ApplicationTheme == ApplicationTheme.Dark;
        set
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsThemeLight)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsThemeDark)));
        }
    }

    public bool IsPlaying
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            if (value)
            {
                _player.Play(((TabItem)tclDomains.SelectedItem)?.Header switch
                {
                    "Extension" => lsvExtensionImages,
                    "Thickness" => lsvThicknessImages,
                    _ => null
                });
            }
            else
            {
                _player.Stop();
            }
        }
    }

    public bool HasImages => ((TabItem)tclDomains.SelectedItem)?.Header switch
    {
        "Extension" => lsvExtensionImages.Items.Count > 0,
        "Thickness" => lsvThicknessImages.Items.Count > 0,
        _ => false
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        LoadExistingThicknessImages();
        LoadExistingExtensionImages();

        DetectCurrentTheme();
    }

    // Internal

    readonly Player _player = new();

    ImageModifier? _modifier = null;
    bool _isDraggingSlider = false;
    Calendar? _calendarDialog = null;
    bool _isUiFrozen = false;

    [System.Runtime.InteropServices.DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
    private static extern bool ShouldSystemUseDarkMode();

    private void LoadExistingThicknessImages()
    {
        var imageFilenames = Directory.EnumerateFiles(
            Path.Combine(Directory.GetCurrentDirectory(), IceThinkness.ImageLocalFolder),
            $"*.png");
        foreach (var imageFilename in imageFilenames)
        {
            LoadThicknessImage(imageFilename, false);
        }
    }

    private void LoadExistingExtensionImages()
    {
        var imageFilenames = Directory.EnumerateFiles(
            Path.Combine(Directory.GetCurrentDirectory(), IceExtension.ImageLocalFolder),
            $"*.png");
        foreach (var imageFilename in imageFilenames)
        {
            LoadExtensionImage(imageFilename, false);
        }
    }

    private void DetectCurrentTheme()
    {
        try
        {
            if (ShouldSystemUseDarkMode())
            {
                IsThemeDark = true;
            }
            else
            {
                IsThemeLight = true;
            }
        }
        catch
        {
            IsThemeLight = true;
        }
    }

    private void ResetView()
    {
        lblIceAmount.Content = "";
    }

    private void UpdateThicknessImage()
    {
        if (imgThickness == null)
            return;

        ImageModifier.PixelOperation operation = ImageModifier.PixelOperation.None;
        if (chkColorizeLand.IsChecked ?? false) operation |= ImageModifier.PixelOperation.ColorizeLand;
        if (chkColorizeRivers.IsChecked ?? false) operation |= ImageModifier.PixelOperation.ColorizeRivers;
        if (chkColorizeSea.IsChecked ?? false) operation |= ImageModifier.PixelOperation.ColorizeSea;
        if (chkColorizeIce.IsChecked ?? false) operation |= ImageModifier.PixelOperation.ColorizeIce;
        /*if (tgsColorizeLand.IsOn) operation |= ImageModifier.PixelOperation.ColorizeLand;
        if (tgsColorizeRivers.IsOn) operation |= ImageModifier.PixelOperation.ColorizeRivers;
        if (tgsColorizeSea.IsOn) operation |= ImageModifier.PixelOperation.ColorizeSea;
        if (tgsColorizeIce.IsOn) operation |= ImageModifier.PixelOperation.ColorizeIce;
        */

        imgThickness.Source = _modifier?.RedrawImage(operation);
    }

    private void UpdateThicknessImageIce()
    {
        //if (tgsColorizeIce.IsOn &&
        if (chkColorizeIce.IsChecked == true &&
            _isDraggingSlider == false &&
            _modifier != null &&
            _modifier.IceColorStep != sldIceColorStep.Value)
        {
            _modifier.IceColorStep = sldIceColorStep.Value;
            UpdateThicknessImage();
        }
    }

    private void SelectThicknessImage(ImageModifier? modifier)
    {
        _modifier = modifier;

        ResetView();
        UpdateThicknessImage();

        if (_modifier != null)
        {
            lblIceAmount.Content = $"{_modifier.GetIceAmount() / 1000:F1}t km3";
        }
    }

    private void SelectExtensionImage(string? imageName)
    {
        if (imageName != null)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imageName);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            imgExtension.Source = bitmap;
        }
        else
        {
            imgExtension.Source = null;
        }
    }

    private void Disable(Label waitingLabel)
    {
        Cursor = Cursors.Wait;
        waitingLabel.Visibility = Visibility.Visible;
        IsEnabled = false;
    }

    private void Enable(Label waitingLabel)
    {
        IsEnabled = true;
        Cursor = Cursors.Arrow;
        waitingLabel.Visibility = Visibility.Collapsed;
    }

    private static ListViewItem? ImageExists(ListView list, string imageName)
    {
        foreach (var item in list.Items)
        {
            var lvi = item as ListViewItem;
            object content = lvi!.Content;
            if (content.Equals(imageName))
            {
                return lvi;
            }
        }

        return null;
    }

    private Calendar.Date[]? SelectDates(DateTime startDate, DateTime? endDate = null)
    {
        Calendar.Date[]? result = null;

        _calendarDialog = new Calendar(startDate, endDate ?? DateTime.Now.AddDays(-1));
        if (_calendarDialog.ShowDialog() == true)
        {
            result = _calendarDialog.Dates;
        }

        _calendarDialog = null;
        return result;
    }

    private bool LoadThicknessImage(string filename, bool prepare)
    {
        bool result = false;

        var imageName = IceThinkness.GetFriendlyImageName(filename);
        var imageItem = ImageExists(lsvThicknessImages, imageName);
        if (imageItem != null)
        {
            imageItem.IsSelected = true;
            return true;
        }

        try
        {
            ImageModifier? modifier = null;
            if (prepare)
            {
                modifier = new ImageModifier(filename)
                {
                    IceColorStep = sldIceColorStep.Value
                };
            }

            lsvThicknessImages.SelectedItem = null;
            lsvThicknessImages.Items.Add(new ListViewItem()
            {
                Content = modifier?.Name ?? imageName,
                Tag = modifier == null ? filename : modifier,
                IsSelected = prepare,
                Padding = new Thickness(8),
                FontSize = 14,
            });

            if (prepare)
            {
                SelectThicknessImage(modifier);
            }

            result = true;
        }
        catch (Exception ex)
        {
            _modifier = null;
            MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return result;
    }


    private bool LoadExtensionImage(string filename, bool select)
    {
        bool result = false;

        var imageName = IceExtension.CreateName(filename);
        var imageItem = ImageExists(lsvExtensionImages, imageName);
        if (imageItem != null)
        {
            imageItem.IsSelected = true;
            return true;
        }

        try
        {
            lsvExtensionImages.SelectedItem = null;
            lsvExtensionImages.Items.Add(new ListViewItem()
            {
                Content = imageName,
                Tag = filename,
                IsSelected = select,
                Padding = new Thickness(8),
                FontSize = 14,
            });

            if (select)
            {
                SelectExtensionImage(filename);
            }

            result = true;
        }
        catch (Exception ex)
        {
            imgExtension.Source = null;
            MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return result;
    }

    private bool DeleteSelectedImages(ListView listView, Action<object> executer)
    {
        var toRemove = new Queue<ListViewItem>();
        try
        {
            foreach (ListViewItem lvi in listView.SelectedItems)
            {
                toRemove.Enqueue(lvi);

                var imageTag = lvi.Tag;
                executer(imageTag);
            }
        }
        catch
        {
            MessageBox.Show("Some files were not removed", Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        bool shouldClearImage = toRemove.Count > 0;

        _isUiFrozen = true;
        while (toRemove.Count > 0)
        {
            var lvi = toRemove.Dequeue();
            listView.Items.Remove(lvi);
        }
        _isUiFrozen = false;

        return shouldClearImage;
    }


    // UI handlers

    private async void ThicknessHyperlink_Click(object sender, RoutedEventArgs e)
    {
        SelectThicknessImage(null);

        //*
        Calendar.Date[]? dates = SelectDates(new DateTime(2000, 1, 1));
        if (dates == null)
            return;

        Disable(lblThicknessWait);

        foreach (var date in dates)
        {
            var filename = await IceThinkness.DownloadImage(date.Year, date.Month, date.Day);
            if (!string.IsNullOrEmpty(filename))
            {
                var fullfilename = Path.GetFullPath(filename);
                LoadThicknessImage(fullfilename, true);
            }
            else
            {
                MessageBox.Show($"Image does not exist on date {date}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*

        Disable(lblThicknessWait);

        var links = await ThinknessImageService.GetLinks();

        Enable(lblThicknessWait);

        if (links == null)
            return;

        _modalDialog = new ChooseImage();
        _modalDialog.SetLinks(links.Reverse().ToArray());
        var selectedImages = _modalDialog.ShowDialog() ?? false ? _modalDialog.GetSelectedImages() : null;
        _modalDialog = null;

        if (selectedImages == null)
            return;

        Disable(lblThicknessWait);

        foreach (var imageName in selectedImages)
        {
            var filename = await ThinknessImageService.DownloadImage(imageName);
            if (!string.IsNullOrEmpty(filename))
            {
                var fullfilename = Path.GetFullPath(filename);
                LoadThicknessImage(fullfilename, true);
            }
        }
        //*/

        Enable(lblThicknessWait);
    }

    private void LoadThicknessImage_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PNG images|*.png"
        };

        if (ofd.ShowDialog() ?? false)
        {
            Disable(lblThicknessWait);
            LoadThicknessImage(ofd.FileName, true);
            Enable(lblThicknessWait);
        }
    }

    private void Colorize_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateThicknessImage();
    }

    private void IceColorThreshold_GotMouseCapture(object sender, MouseEventArgs e)
    {
        _isDraggingSlider = true;
    }

    private void IceColorThreshold_LostMouseCapture(object sender, MouseEventArgs e)
    {
        _isDraggingSlider = false;
        UpdateThicknessImageIce();
    }

    private void IceColorThreshold_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateThicknessImageIce();
    }

    private void ThicknessImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (lsvThicknessImages.SelectedItem != null)
        {
            var lvi = (lsvThicknessImages.SelectedItem as ListViewItem)!;
            var imageTag = lvi.Tag;
            if (imageTag is not ImageModifier modifier)
            {
                Disable(lblThicknessWait);

                modifier = new ImageModifier((string)imageTag)
                {
                    IceColorStep = sldIceColorStep.Value
                };

                Enable(lblThicknessWait);

                lvi.Tag = modifier;
            }

            SelectThicknessImage(modifier);
        }
    }

    private void ThicknessImages_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            bool shouldClearImage = DeleteSelectedImages(lsvThicknessImages, tag =>
            {
                if (tag is ImageModifier modifier)
                {
                    modifier.DeleteFile();
                }
            });

            if (shouldClearImage)
            {
                SelectThicknessImage(null);
            }
        }
    }

    private async void ExtensionHyperlink_Click(object sender, RoutedEventArgs e)
    {
        Calendar.Date[]? dates = SelectDates(new DateTime(1978, 10, 26));
        if (dates == null)
            return;

        Disable(lblExtensionWait);

        foreach (var date in dates)
        {
            var filename = await IceExtension.DownloadImage(date.Year, date.Month, date.Day);
            if (!string.IsNullOrEmpty(filename))
            {
                var fullfilename = Path.GetFullPath(filename);
                LoadExtensionImage(fullfilename, true);
            }
            else
            {
                MessageBox.Show($"Image does not exist on date {date}", Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Enable(lblExtensionWait);
    }

    private void LoadExtensionImage_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PNG images|*.png"
        };

        if (ofd.ShowDialog() ?? false)
        {
            LoadExtensionImage(ofd.FileName, true);
        }
    }

    private void ExtensionImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUiFrozen)
            return;

        if (lsvExtensionImages.SelectedItem != null)
        {
            var lvi = (lsvExtensionImages.SelectedItem as ListViewItem)!;
            var imageTag = lvi.Tag;
            if (imageTag is string imageFilename)
            {
                SelectExtensionImage(imageFilename);
            }
        }
    }

    private void ExtensionImages_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            bool shouldClearImage = DeleteSelectedImages(lsvExtensionImages, tag =>
            {
                if (tag is string imageFilename)
                {
                    File.Delete(imageFilename);
                }
            });

            if (shouldClearImage)
            {
                SelectExtensionImage(null);
            }
        }
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        _calendarDialog?.Activate();
    }

    private void PlayImages_Click(object sender, RoutedEventArgs e)
    {
        IsPlaying = !IsPlaying;
    }

    private void Domains_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasImages)));
    }
}
