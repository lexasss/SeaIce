using ModernWpf;
using SeaIce.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SeaIce;

enum Domain
{
    Unknown,
    Thickness,
    Extension
}

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

    public bool HasImages => this.Domain switch
    {
        Domain.Extension => lsvExtensionImages.Items.Count > 0,
        Domain.Thickness => lsvThicknessImages.Items.Count > 0,
        _ => false
    };

    public int SelectedImageCount => this.Domain switch
    {
        Domain.Extension => lsvExtensionImages.SelectedItems.Count,
        Domain.Thickness => lsvThicknessImages.SelectedItems.Count,
        _ => 0
    };

    public bool IsExtensionDomain => this.Domain == Domain.Extension;

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

    ThicknessImageModifier? _thicknessModifier = null;
    ExtensionImageModifier? _extensionModifier = null;
    bool _isDraggingSlider = false;
    Calendar? _calendarDialog = null;
    bool _isUiFrozen = false;

    private Domain Domain => ((TabItem)tclDomains.SelectedItem)?.Header switch
    {
        "Extension" => Domain.Extension,
        "Thickness" => Domain.Thickness,
        _ => Domain.Unknown
    };

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

    private void ResetThicknessView()
    {
        lblIceAmount.Content = "";
    }

    private void UpdateThicknessImage()
    {
        if (imgThickness == null)
            return;

        ThicknessImageModifier.PixelOperation operation = ThicknessImageModifier.PixelOperation.None;
        if (chkColorizeLand.IsChecked ?? false) operation |= ThicknessImageModifier.PixelOperation.ColorizeLand;
        if (chkColorizeRivers.IsChecked ?? false) operation |= ThicknessImageModifier.PixelOperation.ColorizeRivers;
        if (chkColorizeSea.IsChecked ?? false) operation |= ThicknessImageModifier.PixelOperation.ColorizeSea;
        if (chkColorizeIce.IsChecked ?? false) operation |= ThicknessImageModifier.PixelOperation.ColorizeIce;

        imgThickness.Source = _thicknessModifier?.RedrawImage(operation);
    }

    private void UpdateExtensionImage()
    {
        if (imgExtension == null)
            return;

        imgExtension.Source = _extensionModifier?.Bitmap;
    }

    private void UpdateThicknessImageIce()
    {
        //if (tgsColorizeIce.IsOn &&
        if (chkColorizeIce.IsChecked == true &&
            _isDraggingSlider == false &&
            _thicknessModifier != null &&
            _thicknessModifier.IceColorStep != sldIceColorStep.Value)
        {
            _thicknessModifier.IceColorStep = sldIceColorStep.Value;
            UpdateThicknessImage();
        }
    }

    private void SelectThicknessImage(ThicknessImageModifier? modifier)
    {
        _thicknessModifier = modifier;

        ResetThicknessView();
        UpdateThicknessImage();

        if (_thicknessModifier != null)
        {
            lblIceAmount.Content = $"{_thicknessModifier.GetIceAmount() / 1000:F1}t km3";
        }
    }

    private void SelectExtensionImage(ExtensionImageModifier? modifier /*string? imageName*/)
    {
        _extensionModifier = modifier;
        UpdateExtensionImage();

        /*
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
        }*/
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
            ThicknessImageModifier? modifier = null;
            if (prepare)
            {
                modifier = new ThicknessImageModifier(filename)
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
            _thicknessModifier = null;
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
            ExtensionImageModifier? modifier = null;
            if (select)
            {
                modifier = new ExtensionImageModifier(filename);
            }

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
                SelectExtensionImage(modifier);
                //SelectExtensionImage(filename);
            }

            result = true;
        }
        catch (Exception ex)
        {
            _thicknessModifier = null;
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

    private IImageModifier? EnsureLviTagIsModifier(object? lviObj, Type type, Label wait)
    {
        if (lviObj is not ListViewItem lvi)
            return null;

        var imageTag = lvi.Tag;

        if (imageTag is string tag)
        {
            Disable(wait);

            object modifier;
            if (type == typeof(ExtensionImageModifier))
                modifier = new ExtensionImageModifier(tag);
            else if (type == typeof(ThicknessImageModifier))
                modifier = new ThicknessImageModifier(tag)
                {
                    IceColorStep = sldIceColorStep.Value
                };
            else
            {
                return lvi.Tag as IImageModifier;
            }

            Enable(wait);

            lvi.Tag = modifier;
        }

        return lvi.Tag as IImageModifier;
    }


    // UI event handlers

    private void Window_Activated(object sender, EventArgs e)
    {
        _calendarDialog?.Activate();
    }

    private void Domains_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasImages)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImageCount)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExtensionDomain)));
    }

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

        Enable(lblThicknessWait);
    }

    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PNG images|*.png"
        };

        if (ofd.ShowDialog() ?? false)
        {
            var lblWait = this.Domain == Domain.Thickness ? lblThicknessWait : null;

            if (lblWait != null)
                Disable(lblWait);

            if (this.Domain == Domain.Thickness)
                LoadThicknessImage(ofd.FileName, true);
            else if (this.Domain == Domain.Extension)
                LoadExtensionImage(ofd.FileName, true);

            if (lblWait != null)
                Enable(lblWait);
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImageCount)));

        if (lsvThicknessImages.SelectedItem != null)
        {
            var modifier = EnsureLviTagIsModifier(lsvThicknessImages.SelectedItem, typeof(ThicknessImageModifier), lblThicknessWait);
            if (modifier is ThicknessImageModifier thicknessModifier)
                SelectThicknessImage(thicknessModifier);
        }
    }

    private void ThicknessImages_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            bool shouldClearImage = DeleteSelectedImages(lsvThicknessImages, tag =>
            {
                (tag as IImageModifier)?.DeleteFile();
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

    private void ExtensionImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUiFrozen)
            return;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedImageCount)));

        if (lsvExtensionImages.SelectedItem != null)
        {
            var modifier = EnsureLviTagIsModifier(lsvExtensionImages.SelectedItem, typeof(ExtensionImageModifier), lblExtensionWait);
            if (modifier is ExtensionImageModifier extensionModifier)
                SelectExtensionImage(extensionModifier);
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
                else if (tag is IImageModifier modifier)
                {
                    modifier.DeleteFile();
                }
            });

            if (shouldClearImage)
            {
                SelectExtensionImage(null);
            }
        }
    }

    private void PlayImages_Click(object sender, RoutedEventArgs e)
    {
        IsPlaying = !IsPlaying;
    }

    private void CombineTwoExtensionImages_Click(object sender, RoutedEventArgs e)
    {
        if (lsvExtensionImages.SelectedItems.Count != 2)
            return;

        var modifier1 = EnsureLviTagIsModifier(lsvExtensionImages.SelectedItems[0], typeof(ExtensionImageModifier), lblExtensionWait);
        var modifier2 = EnsureLviTagIsModifier(lsvExtensionImages.SelectedItems[1], typeof(ExtensionImageModifier), lblExtensionWait);

        if (modifier1 != null && modifier2 != null)
        {
            imgExtension.Source = ExtensionImageCombiner.Combine(modifier1.Bitmap, modifier2.Bitmap, modifier1.Name, modifier2.Name);
        }
    }
}