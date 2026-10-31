using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Platform.Surfaces;

class CopyPatches
{
    public class CopyPatch
    {
        public required string path;
        public string? path_from_smg2;
        public string? path_from_smg1;
    }
    public List<CopyPatch> filesToCopy = new();
    public static CopyPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new CopyPatches {filesToCopy = JsonSerializer.Deserialize<List<CopyPatch>>(jsonString, options)!};
    }
    
    public async Task<bool> ApplyAll (string pathToSMG2, string? pathToSMG1, string pathToExternal, string outputPath)
    {
        foreach (var patch in filesToCopy)
        {
            string from;
            if (patch.path_from_smg2 is not null)
            {
                from = Path.Combine(pathToSMG2, patch.path_from_smg2);
            }
            else if (patch.path_from_smg1 is not null)
            {
                if (pathToSMG1 is null) 
                    continue;
                from = Path.Combine(pathToSMG1, patch.path_from_smg1);
            }
            else
            {
                from = Path.Combine(pathToExternal, patch.path);
            }
            string to = Path.Combine(outputPath, patch.path);
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