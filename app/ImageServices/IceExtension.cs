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
    public static string ImageType => "conc";
    public static string ImageResolution => "hires";
    public static string ImageVersion => "4.0";
    public static string ImageLocalFolder => "extension";

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
        string localPath = Path.Combine(ImageLocalFolder, remoteFilename);

        bool isDownloaded = false;
        var token = new CancellationToken();

        using var ftp = new AsyncFtpClient(ServerName);
        await ftp.Connect(token);
        try
        {
            var remotePath = DataPath + remoteFolder + remoteFilename;
            isDownloaded = await ftp.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, token: token) == FtpStatus.Success;
        }
        catch (Exception) { }

        return isDownloaded ? localPath : null;
    }

    // Internal

    private static (string, string) GetImagePath(int year, int month, int day) =>
        ($"{year}/{month:D2}_{Calendar.Monthes[month - 1]}/", $"N_{year}{month:D2}{day:D2}_{ImageType}_{ImageResolution}_v{ImageVersion}.png");
}
