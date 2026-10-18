using Hack.io.RARC;
using Hack.io.MSBT;
using Hack.io.Utility;
using Hack.io.YAZ0;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System;
using System.IO;

class MsbtPatches
{

    public Dictionary<string, Dictionary<string, List<MsbtPatch>>> patches = new();

    public static MsbtPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new MsbtPatches {patches = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<MsbtPatch>>>>(jsonString, options)!};
    }

    public bool ApplyAll (string pathToGame, string pathToTxts, string outputPath)
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
            foreach ((string msbtFile, List<MsbtPatch> patchesPerMsbt) in patchesPerArc)
            {
                MSBT msbt = new();
                RARC.File? msbtInArc = (RARC.File?)(arc[msbtFile] ?? null);
                if (msbtInArc is null) 
                    continue;
                msbt.Load((MemoryStream)msbtInArc);
                foreach (MsbtPatch patch in patchesPerMsbt)
                {
                    patch.Apply(msbt, pathToTxts);
                }
                RARC.File newMsbtInArc = new() { Name = Path.GetFileName(msbtFile) };
                MemoryStream ms = new();
                msbt.Save(ms);
                newMsbtInArc.Load(ms);
                arc[msbtFile] = newMsbtInArc;
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