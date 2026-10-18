using Hack.io.BRK;
using Hack.io.RARC;
using Hack.io.Utility;
using Hack.io.YAZ0;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

class BrkPatches
{
    public Dictionary<string, Dictionary<string, List<BrkPatch>>> patches = new();

    public static BrkPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new BrkPatches {patches = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<BrkPatch>>>>(jsonString, options)!};
    }

    public bool ApplyAll (string pathToGame, string outputPath)
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
            foreach ((string brkFile, List<BrkPatch> patchesPerBrk) in patchesPerArc)
            {
                BRK brk = new();
                RARC.File? brkInArc = (RARC.File?)(arc[brkFile] ?? null);
                if (brkInArc is null) 
                    continue;
                brk.Load((MemoryStream)brkInArc);
                foreach (BrkPatch patch in patchesPerBrk)
                {
                    patch.Apply(brk);
                }
                RARC.File newBrkInArc = new () { Name = Path.GetFileName(brkFile) };
                MemoryStream ms = new();
                brk.Save(ms);
                newBrkInArc.Load(ms);
                arc[brkFile] = newBrkInArc;
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