using System.IO;

namespace ArchipelaGalaxy.Helpers;
class FileHelper
{
    public static void CopyFiles (string inputPath, string outputPath)
    {
        foreach (string dir in Directory.GetDirectories(inputPath))
        {
            Directory.CreateDirectory(dir.Replace(inputPath, outputPath));
            CopyFiles(dir, Path.Combine(outputPath, Path.GetFileName(dir)));
        }
        foreach (string file in Directory.GetFiles(inputPath))
        {
            File.Copy(file, file.Replace(inputPath, outputPath), true);
        }
    }
}