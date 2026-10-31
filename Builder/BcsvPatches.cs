using Hack.io.BCSV;
using Hack.io.RARC;
using Hack.io.Utility;
using Hack.io.YAZ0;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class BcsvPatches
{
    public Dictionary<string, Dictionary<string, List<BcsvPatch>>> patches = new();

    public static BcsvPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new BcsvPatches {patches = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<BcsvPatch>>>>(jsonString, options)!};
    }

    public async Task<bool> ApplyAll (string pathToGame, string outputPath)
    {
        foreach ((string file, var patchesPerArc) in this.patches)
        {
            StreamUtil.PushEndianBig();
            List<(Func<Stream, bool> CheckFunc, Func<byte[], byte[]> DecodeFunction)> DecompFuncs =
            [
                (YAZ0.Check, YAZ0.Decompress)
            ];
            RARC arc = new();
            string pathToFile = Path.Combine(outputPath, file);
            if (!File.Exists(pathToFile))
            {
                pathToFile = Path.Combine(pathToGame, file);
            }
            int encodingselection = FileUtil.LoadFileWithDecompression(pathToFile, arc.Load, [.. DecompFuncs]);
            if (encodingselection == -1)
            {
                FileUtil.LoadFile(pathToFile, arc.Load);
            }
            foreach ((string bcsvFile, List<BcsvPatch> patchesPerBcsv) in patchesPerArc)
            {
                BCSV bcsv = new();
                RARC.File? bcsvInArc = (RARC.File?)(arc[bcsvFile] ?? null);
                if (bcsvInArc is null)
                {
                    Console.WriteLine($"Warning: Couldn't find bcsv file {bcsvFile} in arc, skipping...");
                    continue;
                }
                bcsv.Load((MemoryStream)bcsvInArc);
                foreach (BcsvPatch patch in patchesPerBcsv)
                {
                    patch.Apply(bcsv);
                }
                RARC.File newBcsvInArc = new () { Name = Path.GetFileName(bcsvFile) };
                MemoryStream ms = new();
                bcsv.Save(ms);
                newBcsvInArc.Load(ms);
                arc[bcsvFile] = newBcsvInArc;
            }
            string fileOutPath = Path.Combine(outputPath, file);
            if (!Directory.Exists(Path.GetDirectoryName(fileOutPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileOutPath) ?? throw new NullReferenceException());
            }
            FileUtil.SaveFile(fileOutPath, arc.Save);
        }
        return true;
    }
}