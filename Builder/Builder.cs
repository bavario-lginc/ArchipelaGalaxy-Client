using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

class Builder
{
    public const string PatchPath = "Patch/";
    public const string ModOutputFolder = "Output/Mod";
    public const string ModRootFolderName = "SMG2AP";
    public const string RiivolutionFolderName = "riivolution";
    public static bool IsGeneratedMod ()
    {
        return Directory.Exists(ModOutputFolder);
    }
    private static void CopyFiles (string inputPath, string outputPath)
    {
        foreach (string dir in Directory.GetDirectories(inputPath))
        {
            Directory.CreateDirectory(dir.Replace(inputPath, outputPath));
            CopyFiles(dir, Path.Combine(outputPath, Path.GetFileName(dir)));
        }
        foreach (string file in Directory.GetFiles(inputPath))
        {
            File.Copy(file, file.Replace(inputPath, outputPath), true);
        }
    }
    public static bool GenerateModPreset (string? isoPath, string gameidWithRegion)
    {
        string jsonString = File.ReadAllText(Path.Combine(PatchPath, gameidWithRegion, "ModPreset.json"));
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };
        var preset = JsonSerializer.Deserialize<ModPreset>(jsonString, options);
        if (preset is null || isoPath is null) 
            return false;
        preset.baseFile = isoPath;
        preset.riivolution.patches[0].root = Path.GetFullPath(ModOutputFolder);
        preset.riivolution.patches[0].xml = Path.GetFullPath(Path.Combine(ModOutputFolder, RiivolutionFolderName, gameidWithRegion + ".xml"));
        File.WriteAllText(Path.Combine(ModOutputFolder, "..", "SMG2AP.json"), JsonSerializer.Serialize(preset, options));
        return true;
    }
    public static async Task<bool> GenerateMod (string smg2Path, string? smg1Path, string gameidWithRegion, string outputPath)
    {
        bool fullyGenerated = false;
        string patchPath = Path.Combine(PatchPath, gameidWithRegion);
        if (!Directory.Exists(patchPath))
        {
            bool success = await PatchDownloader.GetPatch(gameidWithRegion);
            if (!success)
                return false;
        }
        string toPath = Path.Combine(ModOutputFolder, ModRootFolderName);
        if (Directory.Exists(toPath))
        {
            Directory.Delete(toPath, true);
        }
        string riivoDest = Path.Combine(ModOutputFolder, RiivolutionFolderName);
        if (Directory.Exists(riivoDest))
        {
            Directory.Delete(riivoDest, true);
        }
        Directory.CreateDirectory(toPath);
        try
        {
            /*if (DolphinHelper.smg1path is not null)
            {
                Console.WriteLine("Copying Ice Mario assets...");
                foreach (string file in SMG1CopyFiles)
                {
                    string from = Path.Combine(DolphinHelper.smg1path, file);
                    string to = Path.Combine(toPath, file);
                    if (!Directory.Exists(Path.GetDirectoryName(from))) 
                        continue;
                    if (!Directory.Exists(Path.GetDirectoryName(to)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                    }
                    File.Copy(from, to);
                }
            }*/

            Console.WriteLine("Applying copy patches...");
            foreach (string copyPatchFile in Directory.GetFiles(Path.Combine(patchPath, "copy")))
            {
                string jsonString = File.ReadAllText(copyPatchFile);
                CopyPatches patches = CopyPatches.Load(jsonString);
                if (!patches.ApplyAll(smg2Path, smg1Path, Path.Combine(patchPath, "copy"), toPath))
                {
                    Console.WriteLine("Stop.");
                    return false;
                }
            }
            Console.WriteLine("Applying arc patches...");
            foreach (string arcPatchFile in Directory.GetFiles(Path.Combine(patchPath, "arc")))
            {
                string jsonString = File.ReadAllText(arcPatchFile);
                RarcPatches patches = RarcPatches.Load(jsonString);
                if (!patches.ApplyAll(smg2Path, Path.Combine(patchPath, "arc"), toPath))
                {
                    Console.WriteLine("Stop.");
                    return false;
                }
            }
            Console.WriteLine("Applying bcsv patches...");
            foreach (string bcsvPatchFile in Directory.GetFiles(Path.Combine(patchPath, "bcsv")))
            {
                string jsonString = File.ReadAllText(bcsvPatchFile);
                BcsvPatches patches = BcsvPatches.Load(jsonString);
                if (!patches.ApplyAll(smg2Path, toPath))
                {
                    Console.WriteLine("Stop.");
                    return false;
                }
            }
            Console.WriteLine("Applying brfnt patches...");
            foreach (string brfntPatchFile in Directory.GetFiles(Path.Combine(patchPath, "brfnt")))
            {
                string jsonString = File.ReadAllText(brfntPatchFile);
                BrfntPatches patches = BrfntPatches.Load(jsonString);
                foreach (string languageDir in Directory.GetDirectories(Path.Combine(patchPath, "msbt")))
                {
                    string curDir = new DirectoryInfo(languageDir).Name ?? throw new NullReferenceException();
                    if (!patches.ApplyAll(Path.Combine(smg2Path, "LocalizeData", curDir), Path.Combine(patchPath, "brfnt"), Path.Combine(toPath, "LocalizeData", curDir)))
                    {
                        Console.WriteLine("Stop.");
                        return false;
                    }
                }
            }
            Console.WriteLine("Applying brk patches...");
            foreach (string brkPatchFile in Directory.GetFiles(Path.Combine(patchPath, "brk")))
            {
                string jsonString = File.ReadAllText(brkPatchFile);
                BrkPatches patches = BrkPatches.Load(jsonString);
                if (!patches.ApplyAll(smg2Path, toPath))
                {
                    Console.WriteLine("Stop.");
                    return false;
                }
            }
            Console.WriteLine("Applying msbf patches...");
            foreach (string msbfPatchFile in Directory.GetFiles(Path.Combine(patchPath, "msbf")))
            {
                string jsonString = File.ReadAllText(msbfPatchFile);
                MsbfPatches patches = MsbfPatches.Load(jsonString);
                foreach (string languageDir in Directory.GetDirectories(Path.Combine(patchPath, "msbt")))
                {
                    string curDir = new DirectoryInfo(languageDir).Name ?? throw new NullReferenceException();
                    if (!patches.ApplyAll(Path.Combine(smg2Path, "LocalizeData", curDir, "MessageData"), Path.Combine(toPath, "LocalizeData", curDir, "MessageData")))
                    {
                        Console.WriteLine("Stop.");
                        return false;
                    }
                }
            }
            Console.WriteLine("Applying msbt patches...");
            string msbtPatchPath = Path.Combine(patchPath, "msbt");
            foreach (string msbtPatchFile in Directory.GetFiles(msbtPatchPath))
            {
                string jsonString = File.ReadAllText(msbtPatchFile);
                MsbtPatches patches = MsbtPatches.Load(jsonString);
                foreach (string languageDir in Directory.GetDirectories(msbtPatchPath))
                {
                    string curDir = new DirectoryInfo(languageDir).Name ?? throw new NullReferenceException();
                    if (!patches.ApplyAll(Path.Combine(smg2Path, "LocalizeData", curDir, "MessageData"), languageDir, Path.Combine(toPath, "LocalizeData", curDir, "MessageData")))
                    {
                        Console.WriteLine("Stop.");
                        return false;
                    }
                }
            }
            Console.WriteLine("Copying Riivolution .xml...");
            Directory.CreateDirectory(riivoDest);
            File.Copy(Path.Combine(PatchPath, gameidWithRegion, gameidWithRegion + ".xml"), Path.Combine(riivoDest, gameidWithRegion + ".xml"));
            Console.WriteLine("Done!");
            fullyGenerated = true;
            return true;
        }
        finally
        {
            if (!fullyGenerated)
            {
                if (Directory.Exists(ModOutputFolder))
                {
                    Directory.Delete(ModOutputFolder, true);
                }
            }
            else
            {
                GenerateModPreset(DolphinHelper.smg2IsoPath, gameidWithRegion);
                CopyFiles(ModOutputFolder, outputPath);
            }
        }
    }
}