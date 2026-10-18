using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using ArchipelaGalaxy.Views;

class PatchDownloader
{
    // 404's currently but once it's public it won't
    public const string PatchDownloadURL = "https://github.com/bavario-lginc/SMG2-Archipelago/releases/download/test1/";
    public const string PatchPath = "../../../Patch/";
    public static async Task<bool> GetPatch (string gameidWithRegion)
    {
        try
        {
            string url = Path.Combine(PatchDownloadURL, gameidWithRegion + ".zip");
            using var client = new HttpClient();
            using var stream = await client.GetStreamAsync(url);
            string dest = Path.Combine(PatchPath, gameidWithRegion);
            if (Directory.Exists(dest))
            {
                Directory.Delete(dest, true);
            }
            ZipFile.ExtractToDirectory(stream, PatchPath);
            return true;
        }
        catch (Exception e)
        {
            var errorWindow = new ErrorWindow("Error while downloading Patch data", e.ToString());
            errorWindow.Show();
            return false;
        }
    }
}