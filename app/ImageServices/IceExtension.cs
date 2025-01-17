using FluentFTP;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SeaIce.ImageServices;

internal static class IceExtension
{
    public static string ServerName = "sidads.colorado.edu";
    public static string DataPath => "/DATASETS/NOAA/G02135/north/daily/images/";
    public static string ImageLocalFolder => "extension";
    public static string[] Monthes => new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    public static int[] Days => new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    static IceExtension()
    {
        if (!Directory.Exists(ImageLocalFolder))
        {
            Directory.CreateDirectory(ImageLocalFolder);
        }
    }

    public static string CreateName(string filename)
    {
        var date = filename.Split('\\')[^1].Split("_")[1];
        return $"{date[0..4]} {date[4..6]} {date[6..]}";
    }

    public static async Task<string?> DownloadImage(int year, int month, int day)
    {
        var (remoteFolder, remoteFilename) = GetImagePath(year, month, day);
        string localFilePath = Path.Combine(ImageLocalFolder, remoteFilename);

        bool isDownloaded = false;
        var token = new CancellationToken();

        using var ftp = new AsyncFtpClient(ServerName);
        await ftp.Connect(token);
        try
        {
            isDownloaded = await ftp.DownloadFile(localFilePath, DataPath + remoteFolder + remoteFilename, FtpLocalExists.Overwrite, token: token) == FtpStatus.Success;
        }
        catch (Exception) { }

        return isDownloaded ? localFilePath : null;
    }

    // Internal

    private static (string, string) GetImagePath(int year, int month, int day) =>
        ($"{year}/{month:D2}_{Monthes[month - 1]}/", $"N_{year}{month:D2}{day:D2}_conc_hires_v3.0.png");
}
