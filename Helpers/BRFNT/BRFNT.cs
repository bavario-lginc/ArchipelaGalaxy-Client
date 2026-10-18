using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RevoFontEditor.System;

namespace RevoFontEditor.Formats;

public partial class BRFNT {
    public u32 SectionInfoSize = 8;
    public u32 MagicRFNT = 0x52464E54;
    public u32 MagicFINF = 0x46494E46;
    public u32 MagicTGPL = 0x54474C50;
    public u32 MagicCWDH = 0x43574448;
    public u32 MagicCMAP = 0x434D4150;
    public u16 BigEndianBOM = 0xFEFF;
    public u16 LittleEndianBOM = 0xFFFE;

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true,
    };

    [JsonIgnore]
    public string FileName { get; set; }
    [JsonIgnore]
    public string FileDirectory { get; set; }

    public bool IsLittleEndian { get; set; }
    public u16 Version { get; set; }

    public FontTextureType TextureType { get; set; }
    public u8 Leading { get; set; }
    public u16 DefaultGlyphIndex { get; set; }
    public u8 DefaultLeftSpacing { get; set; }
    public u8 DefaultGlyphWidth { get; set; }
    public u8 DefaultCharacterWidth { get; set; }
    public FontEncodingType EncodingType { get; set; }
    public u8 Height { get; set; }
    public u8 Width { get; set; }
    public u8 Ascend { get; set; }

    public TGPL GlyphTextureTable { get; set; }
    public List<CWDH> CharacterWidthTables { get; init; }
    public List<CMAP> CharacterMaps { get; init; }

    public BRFNT() {
        FileName = string.Empty;
        FileDirectory = string.Empty;
        GlyphTextureTable = null!;
        CharacterWidthTables = [];
        CharacterMaps = [];
    }

    public void ReadBinary(UltraStream us) {
        var magic = us.ReadValue<u32>(StreamEndian.Big);
        if (magic != MagicRFNT) {

            us.Skip(-4);

            magic = us.ReadValue<u32>(StreamEndian.Little);
            if (magic != MagicRFNT) {
                throw new FormatException("The provided file is not a RFNT font file.");
            }
        }

        var bom = us.ReadValue<u16>(StreamEndian.Big);
        if (bom == LittleEndianBOM) {
            IsLittleEndian = true;
            us.Endian = StreamEndian.Little;
        }
        else {
            us.Endian = StreamEndian.Big;
        }

        Version = us.ReadValue<u16>();
        us.Skip(4);
        var finfOffset = us.ReadValue<u16>();
        us.Skip(2);

        us.Position = finfOffset;

        var finfSection = new SectionInfo();
        finfSection.Read(us);

        if (finfSection.Magic != MagicFINF) {
            throw new FormatException("Invalid FINF section offset.");
        }

        TextureType = us.ReadValue<FontTextureType>();
        Leading = us.ReadValue<u8>();
        DefaultGlyphIndex = us.ReadValue<u16>();
        DefaultLeftSpacing = us.ReadValue<u8>();
        DefaultGlyphWidth = us.ReadValue<u8>();
        DefaultCharacterWidth = us.ReadValue<u8>();
        EncodingType = us.ReadValue<FontEncodingType>();
        var tglpDataOffset = us.ReadValue<u32>();
        var cwdhDataOffset = us.ReadValue<u32>();
        var cmapDataOffset = us.ReadValue<u32>();
        Height = us.ReadValue<u8>();
        Width = us.ReadValue<u8>();
        Ascend = us.ReadValue<u8>();
        us.Skip(1);

        us.Position = tglpDataOffset;
        GlyphTextureTable = new(this);
        GlyphTextureTable.Read(us);

        while (cwdhDataOffset != 0) {
            us.Position = cwdhDataOffset;

            var cwdh = new CWDH();
            cwdhDataOffset = cwdh.Read(us);

            CharacterWidthTables.Add(cwdh);
        }

        while (cmapDataOffset != 0) {
            us.Position = cmapDataOffset;

            var cmap = new CMAP();
            cmapDataOffset = cmap.Read(us);

            CharacterMaps.Add(cmap);
        }
    }

    private void ReadJSON() {
        var info = new ImageInfo() {
            Size = GetEncodedTextureSize(GlyphTextureTable.Format, GlyphTextureTable.Width, GlyphTextureTable.Height),
            Width = GlyphTextureTable.Width,
            Height = GlyphTextureTable.Height,
            Format = GlyphTextureTable.Format,
        };

        foreach (var tex in GlyphTextureTable.Textures) {
            var imgPath = Path.Combine(FileDirectory, tex.Key);
            using var img = Image.Load<Rgba32>(imgPath);

            if (img.Width != info.Width || img.Height != info.Height) {
                throw new FormatException($"Expected a {info.Width}x{info.Height} texture.");
            }

            tex.Value.Encode(img, info);
        }
    }

    public void WriteToBinary(string output) {
        using var us = new UltraStream(endian: IsLittleEndian ? StreamEndian.Little : StreamEndian.Big);
        WriteToBinary(us);

        using var fs = new FileStream(output, FileMode.Create, FileAccess.Write);
        us.Position = 0;
        fs.Position = 0;
        us.CopyTo(fs);
    }

    public void WriteToBinary(UltraStream us) {
        WriteHeader(us, out var refFileSize);
        WriteFINF(us, out var refTGPLOffset, out var refCWDHOffset, out var refCMAPOffset);
        WriteTGPL(us, refTGPLOffset);
        WriteCWHD(us, refCWDHOffset);
        WriteCMAP(us, refCMAPOffset);

        refFileSize.Write((u32)us.Pad(4));
    }

    private void WriteHeader(UltraStream us, out StreamReference<u32> refFileSize) {
        us.WriteValue(MagicRFNT);
        us.WriteValue(IsLittleEndian ? LittleEndianBOM : BigEndianBOM, StreamEndian.Big);
        us.WriteValue(Version);
        refFileSize = us.CreateReference<u32>();
        us.WriteValue<u16>(0x10);
        us.WriteValue((u16)(2 + CharacterWidthTables.Count + CharacterMaps.Count));
    }

    private void WriteFINF(UltraStream us, out StreamReference<u32> refTGPLOffset, out StreamReference<u32> refCWDHOffset, out StreamReference<u32> refCMAPOffset) {
        us.WriteValue(MagicFINF);
        us.WriteValue<u32>(0x20);
        us.WriteValue(TextureType);
        us.WriteValue(Leading);
        us.WriteValue(DefaultGlyphIndex);
        us.WriteValue(DefaultLeftSpacing);
        us.WriteValue(DefaultGlyphWidth);
        us.WriteValue(DefaultCharacterWidth);
        us.WriteValue(EncodingType);
        refTGPLOffset = us.CreateReference<u32>();
        refCWDHOffset = us.CreateReference<u32>();
        refCMAPOffset = us.CreateReference<u32>();
        us.WriteValue(Height);
        us.WriteValue(Width);
        us.WriteValue(Ascend);
        us.Skip(1);
    }

    private void WriteTGPL(UltraStream us, StreamReference<u32> refTGPLOffset) {
        var pos = (u32)us.Pad(4);
        refTGPLOffset.Write(pos + SectionInfoSize);

        var tgplSectionInfo = new SectionInfo();
        tgplSectionInfo.Write(us, MagicTGPL, 0);

        GlyphTextureTable.Write(us);
        tgplSectionInfo.SetSize(us, (u32)us.Position - tgplSectionInfo.Position);
    }

    private void WriteCWHD(UltraStream us, StreamReference<u32> refCWDHOffset) {
        var pos = (u32)us.Pad(4);
        refCWDHOffset.Write(pos + SectionInfoSize);

        StreamReference<u32>? refNextCWDHOffset = null;
        foreach (var cwdh in CharacterWidthTables) {
            var cwdhSectionInfo = new SectionInfo();
            cwdhSectionInfo.Write(us, MagicCWDH, 0);

            refNextCWDHOffset?.Write(cwdhSectionInfo.Position + SectionInfoSize);

            cwdh.Write(us);
            us.Pad(4);

            cwdhSectionInfo.SetSize(us, (u32)us.Position - cwdhSectionInfo.Position);
            refNextCWDHOffset = us.CreateReference<u32>(cwdhSectionInfo.Position + 0xC, false);
        }
    }

    private void WriteCMAP(UltraStream us, StreamReference<u32> refCMAPOffset) {
        var pos = (u32)us.Pad(4);
        refCMAPOffset.Write(pos + SectionInfoSize);

        StreamReference<u32>? refNextCMAPOffset = null;
        foreach (var cmap in CharacterMaps) {
            var cmapSectionInfo = new SectionInfo();
            cmapSectionInfo.Write(us, MagicCMAP, 0);

            refNextCMAPOffset?.Write(cmapSectionInfo.Position + SectionInfoSize);

            cmap.Write(us);
            us.Pad(4);

            cmapSectionInfo.SetSize(us, (u32)us.Position - cmapSectionInfo.Position);
            refNextCMAPOffset = us.CreateReference<u32>(cmapSectionInfo.Position + 0x10, false);
        }
    }

    public void WriteToJson(string directory) {
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(this, SerializerOptions);
        var jsonPath = Path.Combine(directory, FileName + ".json");
        File.WriteAllText(jsonPath, json);

        foreach (var tex in GlyphTextureTable.Textures) {
            using var img = Image.LoadPixelData(tex.Value.DecodedData, GlyphTextureTable.Width, GlyphTextureTable.Height);
            var imgPath = Path.Combine(directory, tex.Key);
            img.SaveAsPng(imgPath);
        }
    }

    public static BRFNT LoadFromBinary(string brfntPath) {
        var font = new BRFNT() {
            FileName = Path.GetFileNameWithoutExtension(brfntPath),
            FileDirectory = Path.GetDirectoryName(brfntPath)!,
        };
        using var fs = File.OpenRead(brfntPath);
        using var us = UltraStream.CreateFrom(fs);
        font.ReadBinary(us);
        return font;
    }

    public static BRFNT LoadFromJSON(string jsonPath) {
        var json = File.ReadAllText(jsonPath);
        var font = JsonSerializer.Deserialize<BRFNT>(json, SerializerOptions)!;
        font.FileName = Path.GetFileNameWithoutExtension(jsonPath);
        font.FileDirectory = Path.GetDirectoryName(jsonPath)!;
        font.ReadJSON();
        return font;
    }
}