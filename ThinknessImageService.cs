using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SeaIce;

internal static class ThinknessImageService
{
    public static string ImageSource => "https://ocean.dmi.dk/arctic/icethickness/anim/plots_uk/";
    public static string ImageLocalFolder => "thickness";

    static ThinknessImageService()
    {
        if (!Directory.Exists(ImageLocalFolder))
        {
            Directory.CreateDirectory(ImageLocalFolder);
        }
    }

    public static string CreateName(string filename)
    {
        var date = filename.Split('\\')[^1].Split("_")[^1].Split(".")[0];
        return $"{date[0..4]} {date[4..6]} {date[6..]}";
    }

    public static async Task<string[]?> GetLinks()
    {
        string[]? result = null;

        HttpClient client = new();
        var response = await client.GetAsync(ImageSource);
        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var re = new Regex($"\"{ImageBasename}(\\d+).png");
            var matches = re.Matches(content);
            result = matches.Select(match => match.ToString()[1..]).ToArray();
        }

        return result;
    }

    public static async Task<string?> DownloadImage(string imageName)
    {
        string? filename = null;

        var client = new HttpClient();
        var response = await client.GetAsync(ImageSource + imageName);
        if (response?.StatusCode == System.Net.HttpStatusCode.OK)
        {
            filename = Path.Combine(ImageLocalFolder, imageName.Split("/")[^1]);
            var content = await response.Content.ReadAsByteArrayAsync();
            using var writer = new StreamWriter(filename);
            await writer.BaseStream.WriteAsync(content);
        }

        return filename;
    }

    // Internal

    private static string ImageBasename => "CICE_combine_thick_SM_EN_"; // "FullSize_CICE_combine_thick_SM_EN_";
}
