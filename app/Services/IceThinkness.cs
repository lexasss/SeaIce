using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SeaIce.Services;

internal static class IceThinkness
{
    public static string ImageSource => "https://ocean.dmi.dk/arctic/icethickness/anim/plots_uk/";
    public static string ImageLocalFolder => "thickness";

    static IceThinkness()
    {
        if (!Directory.Exists(ImageLocalFolder))
        {
            Directory.CreateDirectory(ImageLocalFolder);
        }
    }

    public static string GetFriendlyImageName(string filename)
    {
        var date = filename.Split('\\')[^1].Split("_")[^1].Split(".")[0];
        return $"{date[0..4]} {date[4..6]} {date[6..]}";
    }

    /*public static async Task<string[]?> GetLinks()
    {
        string[]? result = null;

        HttpClient client = new();
        var response = await client.GetAsync(ImageSource);
        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var re = new System.Text.RegularExpressions.Regex($"\"{ImageBasename}(\\d+).png");
            var matches = re.Matches(content);
            result = matches.Select(match => match.ToString()[1..]).ToArray();
        }

        return result;
    }

    public static async Task<string?> DownloadImage(string imageName)
    {
        string? filename = null;

        using var client = new HttpClient();
        var response = await client.GetAsync(ImageSource + imageName);
        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
        {
            filename = Path.Combine(ImageLocalFolder, imageName.Split("/")[^1]);
            var content = await response.Content.ReadAsByteArrayAsync();
            using var writer = new StreamWriter(filename);
            await writer.BaseStream.WriteAsync(content);
        }

        return filename;
    }*/

    public static async Task<string?> DownloadImage(int year, int month, int day)
    {
        string? localFilePath = null;

        var remoteFilename = GetImageImage(year, month, day);

        using var client = new HttpClient();
        var response = await client.GetAsync(ImageSource + remoteFilename);
        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
        {
            localFilePath = Path.Combine(ImageLocalFolder, remoteFilename);
            var content = await response.Content.ReadAsByteArrayAsync();
            using var writer = new StreamWriter(localFilePath);
            await writer.BaseStream.WriteAsync(content);
        }

        return localFilePath;
    }

    // Internal

    private static string ImageBasename => "CICE_combine_thick_SM_EN_"; // "FullSize_CICE_combine_thick_SM_EN_";

    private static string GetImageImage(int year, int month, int day) => $"{ImageBasename}{year}{month:D2}{day:D2}.png";
}
