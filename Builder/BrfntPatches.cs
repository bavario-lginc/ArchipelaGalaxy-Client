using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hack.io.RARC;
using Hack.io.Utility;
using Hack.io.YAZ0;
using RevoFontEditor.Formats;
using RevoFontEditor.System;

class BrfntPatches
{
    public Dictionary<string, Dictionary<string, List<BrfntPatch>>> patches = new();
    
    public static BrfntPatches Load (string jsonString)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return new BrfntPatches {patches = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<BrfntPatch>>>>(jsonString, options)!};
    }

    private BRFNT LoadFromMemoryStream (MemoryStream strm, string filePathInArc)
    {
        var brfnt = new BRFNT()
        {
            FileName = Path.GetFileNameWithoutExtension(filePathInArc),
            FileDirectory = Path.GetDirectoryName(filePathInArc)!
        };
        using var us = UltraStream.CreateFrom(strm);
        brfnt.ReadBinary(us);
        return brfnt;
    }

    private MemoryStream WriteToMemoryStream (BRFNT brfnt)
    {
        using var us = new UltraStream(endian: StreamEndian.Big);
        brfnt.WriteToBinary(us);
        MemoryStream ms = new();
        us.Position = 0;
        ms.Position = 0;
        us.CopyTo(ms);
        return ms;
    }

    public async Task<bool> ApplyAll (string pathToGame, string pathToTextures, string outputPath)
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
            foreach ((string brfntFile, List<BrfntPatch> patchesPerBrfnt) in patchesPerArc)
            {
                RARC.File? brfntInArc = (RARC.File?)(arc[brfntFile] ?? null);
                if (brfntInArc is null) 
                    continue;
                BRFNT brfnt = LoadFromMemoryStream((MemoryStream)brfntInArc, brfntFile);
                foreach (BrfntPatch patch in patchesPerBrfnt)
                {
                    patch.Apply(brfnt, pathToTextures);
                }
                RARC.File newBrfntInArc = new() { Name = Path.GetFileName(brfntFile) };
                MemoryStream ms = WriteToMemoryStream(brfnt);
                newBrfntInArc.Load(ms);
                arc[brfntFile] = newBrfntInArc;
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