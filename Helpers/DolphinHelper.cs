using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        "C:\\Program Files\\Dolphin",
        "C:\\Program Files\\Dolphin-x64",
        "C:\\Program Files\\Dolphin Emulator",
        "C:\\Program Files (x86)\\Dolphin",
		"C:\\Program Files (x86)\\Dolphin-x64",
		"C:\\Program Files (x86)\\Dolphin Emulator"
    ];
    public static readonly string[] CommonDolphinToolPaths_Windows = CommonDolphinPaths_Windows;
    public static readonly string[] CommonDolphinPaths_Linux = [
        "/usr/bin/",
        "/usr/local/bin/"
    ];
    public static readonly string[] CommonDolphinToolPaths_Linux = [
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
    public static (string?, string?) GetDolphinExePaths (string? path)
    {
        if (path is null) 
            return (null, null);
        foreach (string dolphinExeName in DolphinExeNames)
        {
            string currentDolExe = Path.Combine(path, dolphinExeName);
            if (File.Exists(currentDolExe))
            {
                foreach (string dolphinToolExeName in DolphinToolExeNames)
                {
                    string currentToolExe = Path.Combine(path, dolphinToolExeName);
                    if (File.Exists(currentToolExe)) 
                        return (currentDolExe, currentToolExe);
                }
                continue;
            }
        }
        return (null, null);
    }
    public static (string?, string?) GetDolphinExePaths (string[] paths)
    {
        (string? dolExe, string? toolExe) = (null, null);
        foreach (string path in paths)
        {
            (string? currentDolExe, string? currentToolExe) = GetDolphinExePaths(path);
            (dolExe, toolExe) = (currentDolExe ?? dolExe, currentToolExe ?? toolExe);
            if ((dolExe, toolExe) is not (null, null)) 
                return (dolExe, toolExe);
        }
        return (dolExe, toolExe);
    }
    public static bool ExtractIso (string isoPath, string outputPath)
    {
        if (DolphinPath is null)
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
            if (DolphinToolPath is null)
                return false;
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
            char region = 'H';
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
    public static (string?, string?) GetDolphinPaths ()
    {
        if (DolphinPath is not null && DolphinToolPath is not null)
            return (DolphinPath, DolphinToolPath);
        if (Config.data!.DolphinPath is not null && Config.data.DolphinToolPath is not null)
        {
            dolphinPath = Config.data.DolphinPath;
            dolphinToolPath = Config.data.DolphinToolPath;
            return (dolphinPath, dolphinToolPath);
        }
        // Try grabbing running process
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
            (DolphinPath, DolphinToolPath) = GetDolphinExePaths(pathToDolphinDir);
            if ((DolphinPath, DolphinToolPath) is (null, null))
                continue;
            return (DolphinPath, DolphinToolPath);
        }
        // Otherwise: Check common paths
        string[] CommonDolphinPaths = [];
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            CommonDolphinPaths = CommonDolphinPaths_Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            CommonDolphinPaths = CommonDolphinPaths_Linux;
        }
        (DolphinPath, DolphinToolPath) = GetDolphinExePaths(CommonDolphinPaths);
        if ((DolphinPath, DolphinToolPath) is (null, null))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                CommonDolphinPaths = CommonDolphinPaths_Flatpak;
                (DolphinPath, DolphinToolPath) = GetDolphinExePaths(CommonDolphinPaths);
                if ((DolphinPath, DolphinToolPath) is not (null, null))
                    isFlatpak = true;
                return (DolphinPath, DolphinToolPath);
            }
        }
        return (DolphinPath, DolphinToolPath);
    }

    public static void LaunchMod ()
    {
        var path = GetDolphinPaths().Item1;
        if (path is null)
            return;
        Process dolphin;
        List<string> args = ["-e", Path.Combine(OutputPath, "SMG2AP.json"), "-C", "Dolphin.Core.RAMOverrideEnable=False"];
        if (isFlatpak)
        {
            dolphin = Process.Start("flatpak", ["run", "--command=dolphin-emu", "org.DolphinEmu.dolphin-emu", .. args]);
        }
        else
        {
            dolphin = Process.Start(path, args);
        }
    }
}