using Hack.io.RARC;
using Hack.io.MSBF;
using Hack.io.Utility;
using Hack.io.YAZ0;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;

class MsbfPatches
{

    public Dictionary<string, Dictionary<string, List<MsbfPatch>>> patches = new();

    public static MsbfPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new MsbfPatches {patches = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<MsbfPatch>>>>(jsonString, options)!};
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
            foreach ((string msbfFile, List<MsbfPatch> patchesPerMsbf) in patchesPerArc)
            {
                MSBF msbf = new();
                RARC.File? msbfInArc = (RARC.File?)(arc[msbfFile] ?? null);
                if (msbfInArc is null) 
                    continue;
                msbf.Load((MemoryStream)msbfInArc);
                foreach (MsbfPatch patch in patchesPerMsbf)
                {
                    patch.Apply(msbf);
                }
                RARC.File newMsbfInArc = new() { Name = Path.GetFileName(msbfFile) };
                MemoryStream ms = new();
                msbf.Save(ms);
                newMsbfInArc.Load(ms);
                arc[msbfFile] = newMsbfInArc;
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