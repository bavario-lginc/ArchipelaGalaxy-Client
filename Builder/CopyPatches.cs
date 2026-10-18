using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class CopyPatches
{
    public List<string> filesToCopy = new();
    public static CopyPatches Load (string jsonString)
    {
        return new CopyPatches {filesToCopy = JsonSerializer.Deserialize<List<string>>(jsonString)!};
    }
    
    public bool ApplyAll (string pathToExternal, string outputPath)
    {
        foreach (string file in filesToCopy)
        {
            string from = Path.Combine(pathToExternal, file);
            string to = Path.Combine(outputPath, file);
            string? toDirName = Path.GetDirectoryName(to);
            if (toDirName is not null && !Directory.Exists(toDirName))
            {
                Directory.CreateDirectory(toDirName);
            }
            File.Copy(from, to);
        }
        return true;
    }
}