using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using ArchipelaGalaxy.Views;
using Avalonia.Utilities;

class DolphinHelper
{
    public static readonly string OutputPath = "Output";
    public static readonly char[] ValidSMG2Regions = ['E', 'P', 'J', 'K', 'W'];
    public const string SMG2GameId = "SB4";
    public static readonly char[] ValidSMG1Regions = ['E', 'P', 'J', 'K'];
    public const string SMG1GameId = "RMG";
    public static readonly string[] DolphinExeNames = ["Dolphin.exe", "dolphin-emu", "Dolphin", "dolphin"];
    public static readonly string[] DolphinToolExeNames = ["DolphinTool.exe", "DolphinTool", "dolphin-tool"];
    public static readonly string[] ExtractCommand = ["extract", "-p", "DATA"];
    public static readonly string[] SupportedExtensions = ["*.iso", "*.wbfs", "*.rvz", "*.gcz", "*.wia"];
    public const string DolphinToolDownloadURL = "https://github.com/bavario-lginc/DolphinToolExecutables/releases/latest/download";

    public static string? smg2path;
    public static string? smg2IsoPath;
    public static string? smg2GameIdWithRegion;
    public static string? smg1path;
    public static string? smg1IsoPath;
    public static string? smg1GameIdWithRegion;
    private static string? dolphinPath;
    public static string? DolphinPath {
        get => dolphinPath;
        set {
            dolphinPath = value;
            Config.data!.DolphinPath = value;
            Config.Save();
        }
    }
    private static string? dolphinToolPath;
    public static string? DolphinToolPath {
        get => dolphinToolPath;
        set {
            dolphinToolPath = value;
            Config.data!.DolphinToolPath = value;
            Config.Save();
        }
    }
    public static bool isFlatpak = false;
    
    public static readonly string[] CommonDolphinPaths_Windows = [
        "Output",
        "C:\\Program Files\\Dolphin",
        "C:\\Program Files\\Dolphin-x64",
        "C:\\Program Files\\Dolphin Emulator",
        "C:\\Program Files (x86)\\Dolphin",
		"C:\\Program Files (x86)\\Dolphin-x64",
		"C:\\Program Files (x86)\\Dolphin Emulator"
    ];
    public static readonly string[] CommonDolphinToolPaths_Windows = CommonDolphinPaths_Windows;
    public static readonly string[] CommonDolphinPaths_Linux = [
        "Output",
        "/usr/bin/",
        "/usr/local/bin/"
    ];
    public static readonly string[] CommonDolphinToolPaths_Linux = [
        "Output",
        "/usr/bin/",
        "/usr/local/bin/"
    ];
    public static readonly string[] CommonDolphinPaths_Flatpak = [
        "/var/lib/flatpak/app/org.DolphinEmu.dolphin-emu/current/active/files/bin/",
        "~/.local/share/flatpak/app/org.DolphinEmu.dolphin-emu/current/active/files/bin/"
    ];

    public static string? GetSMG2RegionPretty ()
    {
        return smg2GameIdWithRegion switch
        {
            "SB4E" => "USA",
            "SB4P" => "PAL",
            "SB4J" => "JPN",
            "SB4K" => "KOR",
            "SB4W" => "TWN",
            _ => null
        };
    }

    public static string? GetSMG1RegionPretty ()
    {
        return smg1GameIdWithRegion switch
        {
            "RMGE" => "USA",
            "RMGP" => "PAL",
            "RMGJ" => "JPN",
            "RMGK" => "KOR",
            _ => null
        };
    }

    public static void Init ()
    {
        InitDolphinToolPath();
        InitDolphinPath();
        if (!Directory.Exists(OutputPath))
        {
            Directory.CreateDirectory(OutputPath);
        }
        foreach (string dir in Directory.GetDirectories(OutputPath))
        {
            string? gameid = Path.GetFileName(dir);
            if (gameid == null) 
                continue;
            if (smg2path is null && gameid.Contains(SMG2GameId) && gameid.ContainsAny(ValidSMG2Regions))
            {
                smg2path = Path.Combine(dir, "DATA", "files");
                smg2GameIdWithRegion = gameid;
                smg2IsoPath = Config.data!.SMG2IsoPath;
                continue;
            }
            if (smg1path is null && gameid.Contains(SMG1GameId) && gameid.ContainsAny(ValidSMG1Regions))
            {
                smg1path = Path.Combine(dir, "DATA", "files");
                smg1GameIdWithRegion = gameid;
                continue;
            }
        }
    }
    
    public static (string?, string?) GetDolphinPathsFromFolder (string? path)
    {
        if (path is null) 
            return (null, null);
        string? dolphinPath = null;
        string? dolphinToolPath = null;
        foreach (string dolphinExeName in DolphinExeNames)
        {
            string currentExe = Path.Combine(path, dolphinExeName);
            if (File.Exists(currentExe))
            {
                dolphinPath = currentExe;
                break;
            }
        }
        foreach (string dolphinToolExeName in DolphinToolExeNames)
        {
            string currentExe = Path.Combine(path, dolphinToolExeName);
            if (File.Exists(currentExe))
            {
                dolphinToolPath = currentExe;
                break;
            }
        }
        return (dolphinPath, dolphinToolPath);
    }
    public static (string?, string?) GetDolphinPathsFromFolder (string[] paths)
    {
        (string? dolExe, string? toolExe) = (null, null);
        foreach (string path in paths)
        {
            (string? currentDolExe, string? currentToolExe) = GetDolphinPathsFromFolder(path);
            (dolExe, toolExe) = (currentDolExe ?? dolExe, currentToolExe ?? toolExe);
            if (dolExe is not null && toolExe is not null) 
                break;
        }
        return (dolExe, toolExe);
    }

    public static bool ExtractIso (string isoPath, string outputPath)
    {
        if (DolphinToolPath is null)
            return false;
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }
        List<string> command = [.. ExtractCommand, "-i", isoPath, "-o", outputPath];
        if (isFlatpak)
        {
            using Process dolphinTool = Process.Start("flatpak", ["run", "--command=dolphin-tool", "org.DolphinEmu.dolphin-emu", .. command]);
            dolphinTool.WaitForExit();
            return dolphinTool.ExitCode == 0;
        }
        else
        {
            using Process dolphinTool = Process.Start(DolphinToolPath, command);
            dolphinTool.WaitForExit();
            return dolphinTool.ExitCode == 0;
        }
    }
    private static (string?, string?) Load (string isoPath, string gameid, char[] validRegions)
    {
        string outputPath = Path.GetFullPath(Path.Combine(OutputPath, gameid));
        try
        {
            if (!ExtractIso(isoPath, outputPath)) 
                return (null, null);

            string bootBinPath = Path.Combine(outputPath, "DATA", "sys", "boot.bin");
            if (!File.Exists(bootBinPath))
                return (null, null);
            char region = 'h';
            using (FileStream fs = new(bootBinPath, FileMode.Open))
            {
                foreach (char gameIdLetter in gameid)
                {
                    if (fs.ReadByte() != gameIdLetter)
                    {
                        Console.WriteLine($"Error: Unknown gameid. Cannot use ISO.");
                        return (null, null);
                    }
                }
                region = (char)fs.ReadByte();
            }
            foreach (char regionLetter in validRegions)
            {
                if (region == regionLetter)
                {
                    string newOutputPath = Path.GetFullPath(Path.Combine(OutputPath, gameid + region));
                    if (Directory.Exists(newOutputPath))
                    {
                        Directory.Delete(newOutputPath, true);
                    }
                    Directory.Move(outputPath, newOutputPath);
                    return (Path.Combine(newOutputPath, "DATA", "files"), gameid + region);
                }
            }
            Console.WriteLine($"Error: Unknown region {region}. Cannot use ISO.");
            return (null, null);
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }
    }
    public static string? LoadSMG2 (string isoPath)
    {
        (smg2path, smg2GameIdWithRegion) = Load(isoPath, SMG2GameId, ValidSMG2Regions);
        if (smg2path is not null)
        {
            smg2IsoPath = isoPath;
            Config.data!.SMG2IsoPath = isoPath;
            Config.Save();
        }
        return smg2path;
    }
    public static string? LoadSMG1 (string isoPath)
    {
        (smg1path, smg1GameIdWithRegion) = Load(isoPath, SMG1GameId, ValidSMG1Regions);
        if (smg1path is not null)
        {
            smg1IsoPath = isoPath;
        }
        return smg1path;
    }

    public static bool TryGetDolphinPathsWhileRunning ()
    {
        List<Process> pname = [];
        foreach (string dolphinExeName in DolphinExeNames)
        {
            var processes = Process.GetProcessesByName(dolphinExeName);
            foreach (var process in processes)
            {
                pname.Add(process);
            }
        }
        foreach (Process process in pname)
        {
            if (process.MainModule is null)
                continue;
            string? pathToDolphinDir = Path.GetDirectoryName(process.MainModule.FileName);
            if (pathToDolphinDir is null) 
                continue;
            (DolphinPath, DolphinToolPath) = GetDolphinPathsFromFolder(pathToDolphinDir);
            if ((DolphinPath, DolphinToolPath) is not (null, null))
                return true;
        }
        return false;
    }
    public static bool TryGetDolphinPathsCommonPaths ()
    {
        string[] CommonDolphinPaths = [];
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            CommonDolphinPaths = CommonDolphinPaths_Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            CommonDolphinPaths = CommonDolphinPaths_Linux;
        }
        (DolphinPath, DolphinToolPath) = GetDolphinPathsFromFolder(CommonDolphinPaths);
        if ((DolphinPath, DolphinToolPath) is (null, null))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CommonDolphinPaths = CommonDolphinPaths_Flatpak;
                (DolphinPath, DolphinToolPath) = GetDolphinPathsFromFolder(CommonDolphinPaths);
                if ((DolphinPath, DolphinToolPath) is (null, null))
                    return false;
                isFlatpak = true;
            }
        }
        return true;
    }
    public static bool InitDolphinToolPath ()
    {
        if (DolphinToolPath is not null)
            return true;
        if (Config.data!.DolphinToolPath is not null)
        {
            dolphinToolPath = Config.data.DolphinToolPath;
            return true;
        }
        if (TryGetDolphinPathsWhileRunning() && DolphinToolPath is not null)
            return true;
        if (TryGetDolphinPathsCommonPaths() && DolphinToolPath is not null) 
            return true;
        return false;
    }
    public static bool InitDolphinPath ()
    {
        if (DolphinPath is not null)
            return true;
        if (Config.data!.DolphinPath is not null)
        {
            dolphinPath = Config.data.DolphinPath;
            return true;
        }
        if (TryGetDolphinPathsWhileRunning() && DolphinPath is not null)
            return true;
        if (TryGetDolphinPathsCommonPaths() && DolphinPath is not null) 
            return true;
        return false;
    }

    public static async Task<bool> DownloadDolphinToolAsync ()
    {
        string downloadPath = "dolphin-tool-";
        downloadPath += RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64-",
            Architecture.Arm => "arm-",
            Architecture.Arm64 => "arm64-",
            _ => throw new PlatformNotSupportedException("There is no DolphinTool hosted for your architecture."),
        };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            downloadPath += "win32.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            downloadPath += "darwin";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            downloadPath += "linux";
        }
        downloadPath = Path.Combine(DolphinToolDownloadURL, downloadPath);
        
        try
        {
            using var client = new HttpClient();
            using var stream = await client.GetStreamAsync(downloadPath);
            string dest = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dest = Path.Combine(OutputPath, "DolphinTool.exe");
            }
            else
            {
                dest = Path.Combine(OutputPath, "dolphin-tool");    
            }
            using var executable = File.Create(dest);
            stream.CopyTo(executable);
            DolphinToolPath = dest;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("chmod", ["+x", dest]);
            }
            return true;
        }
        catch (Exception e)
        {
            var errorWindow = new ErrorWindow("Error while downloading DolphinTool", e.ToString());
            errorWindow.Show();
            return false;
        }
    }

    public static void LaunchMod ()
    {
        InitDolphinPath();
        if (DolphinPath is null)
            return;
        Process dolphin;
        List<string> args = ["-e", Path.Combine(OutputPath, "SMG2AP.json"), "-C", "Dolphin.Core.RAMOverrideEnable=False"];
        if (isFlatpak)
        {
            dolphin = Process.Start("flatpak", ["run", "--command=dolphin-emu", "org.DolphinEmu.dolphin-emu", .. args]);
        }
        else
        {
            dolphin = Process.Start(DolphinPath, args);
        }
    }
}