using Hack.io.RARC;
using Hack.io.Utility;
using Hack.io.YAZ0;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

class RarcPatches
{
    public Dictionary<string, List<RarcPatch>> patches = new();
    public static RarcPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new RarcPatches {patches = JsonSerializer.Deserialize<Dictionary<string, List<RarcPatch>>>(jsonString, options)!};
    }

    public bool ApplyAll (string pathToGame, string pathToExternal, string outputPath)
    {
        foreach ((string file, List<RarcPatch> patches) in this.patches)
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
            foreach (RarcPatch patch in patches)
            {
                patch.Apply(arc, pathToExternal);
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