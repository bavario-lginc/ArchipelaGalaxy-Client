using System.IO;
using Hack.io.RARC;

class RarcPatch
{
    public enum ActionType
    {
        AddFile,
        RemoveFile
    }
    public ActionType action;
    public string path_in_arc = "";
    public string? external_path;

    public bool Apply (RARC arc, string pathToExternal)
    {
        switch (action)
        {
            case ActionType.AddFile:
                if (external_path is null) 
                    return false;
                var file = new RARC.File();
                FileStream fs = File.Open(Path.Combine(pathToExternal, external_path), FileMode.Open);
                file.Load(fs);
                arc[path_in_arc] = file;
                fs.Close();
                break;
            case ActionType.RemoveFile:
                arc[path_in_arc] = null;
                break;
        }
        return true;
    }
}