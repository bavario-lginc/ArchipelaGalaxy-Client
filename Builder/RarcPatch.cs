using System;
using System.Collections.Generic;
using System.IO;
using Hack.io.RARC;
using Hack.io.Utility;
using Hack.io.YAZ0;

class RarcPatch
{
    public enum ActionType
    {
        AddFile,
        RemoveFile,
        CreateFromGame
    }
    public required ActionType action;
    public string? root_name;
    public string? path_in_arc;
    public string? external_path;
    public string? game_path;
    public string? game_path_in_arc;

    private void LoadFile (RARC arc, string pathToFile)
    {
        List<(Func<Stream, bool> CheckFunc, Func<byte[], byte[]> DecodeFunction)> DecompFuncs =
        [
            (YAZ0.Check, YAZ0.Decompress)
        ];
        int encodingselection = FileUtil.LoadFileWithDecompression(pathToFile, arc.Load, [.. DecompFuncs]);
        if (encodingselection == -1)
        {
            FileUtil.LoadFile(pathToFile, arc.Load);
        }
    }

    public bool Apply (RARC arc, string pathToExternal, string pathToGame)
    {
        switch (action)
        {
            case ActionType.AddFile:
                if ((external_path is null && game_path is null) || path_in_arc is null) 
                    return false;
                var file = new RARC.File();
                FileStream fs;
                if (game_path is not null)
                {
                    string fromPath = Path.Combine(pathToGame, game_path);
                    if (!File.Exists(fromPath))
                        return false;
                    if (game_path_in_arc is not null)
                    {
                        RARC from = new();
                        LoadFile(from, fromPath);
                        arc[path_in_arc] = from[game_path_in_arc];
                        return true;
                    }
                    fs = File.Open(fromPath, FileMode.Open);
                }
                else
                {
                    fs = File.Open(Path.Combine(pathToExternal, external_path!), FileMode.Open);
                }
                file.Load(fs);
                arc[path_in_arc] = file;
                fs.Close();
                break;
            case ActionType.RemoveFile:
                if (path_in_arc is null) 
                    return false;
                arc[path_in_arc] = null;
                break;
        }
        return true;
    }
}