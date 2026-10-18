using RevoFontEditor.System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RevoFontEditor.Formats;

public partial class BRFNT {
    // TGLP
    public class TGPL {
        [JsonIgnore]
        public BRFNT Font { get; }
        public u8 GlyphCellWidth { get; set; }
        public u8 GlyphCellHeight { get; set; }
        public s8 Baseline { get; set; }
        public u8 MaxCharacterWidth { get; set; }
        public TextureFormat Format { get; set; }
        public u16 GlyphsPerRow { get; set; }
        public u16 GlyphsPerColumn { get; set; }
        public u16 Width { get; set; }
        public u16 Height { get; set; }
        [JsonIgnore]
        public Dictionary<string, TGPLTexture> Textures { get; }
        public IEnumerable<string> TextureNames {
            get => Textures.Keys;
            set {
                Textures.Clear();

                foreach (var name in value) {
                    Textures.Add(name, new TGPLTexture(this));
                }
            }
        }

        public TGPL(BRFNT font) {
            Font = font;
            Textures = [];
        }

        public void Read(UltraStream us) {
            GlyphCellWidth = us.ReadValue<u8>();
            GlyphCellHeight = us.ReadValue<u8>();
            Baseline = us.ReadValue<s8>();
            MaxCharacterWidth = us.ReadValue<u8>();
            var textureSize = us.ReadValue<s32>();
            var textureCount = us.ReadValue<u16>();
            Format = us.ReadValue<TextureFormat>();
            GlyphsPerRow = us.ReadValue<u16>();
            GlyphsPerColumn = us.ReadValue<u16>();
            Width = us.ReadValue<u16>();
            Height = us.ReadValue<u16>();
            var textureOffset = us.ReadValue<u32>();

            var info = new ImageInfo() {
                Size = textureSize,
                Width = Width,
                Height = Height,
                Format = Format,
            };

            using (us.TemporarySeek(textureOffset)) {
                for (var i = 0; i < textureCount; i++) {
                    var textureName = $"{Font.FileName}.{i}.png";
                    var texture = new TGPLTexture(this);
                    texture.Decode(us, info);
                    Textures.Add(textureName, texture);
                }
            }
        }

        public void Write(UltraStream us) {
            us.WriteValue(GlyphCellWidth);
            us.WriteValue(GlyphCellHeight);
            us.WriteValue(Baseline);
            us.WriteValue(MaxCharacterWidth);
            us.WriteValue(GetEncodedTextureSize(Format, Width, Height));
            us.WriteValue((u16)Textures.Count);
            us.WriteValue(Format);
            us.WriteValue(GlyphsPerRow);
            us.WriteValue(GlyphsPerColumn);
            us.WriteValue(Width);
            us.WriteValue(Height);
            var refTextureOffset = us.CreateReference<u32>();
            refTextureOffset.Write((u32)us.Pad(0x20));

            foreach (var texture in Textures) {
                us.Write(texture.Value.EncodedData);
            }
        }
    }

    public class TGPLTexture {
        public TGPL Table { get; }
        public u8[] EncodedData { get; private set; }
        public Rgba32[] DecodedData { get; private set; }

        public TGPLTexture(TGPL table) {
            Table = table;
            EncodedData = [];
            DecodedData = [];
        }

        public void Encode(Image<Rgba32> image, ImageInfo info) {
            DecodedData = new Rgba32[info.Width * info.Height * 4];
            image.CopyPixelDataTo(DecodedData);

            EncodedData = new u8[info.Size];
            EncodeTexture(DecodedData, info, EncodedData);
        }

        public void Decode(UltraStream us, ImageInfo info) {
            EncodedData = new u8[info.Size];
            us.GetSpan((s32)us.Position, info.Size).CopyTo(EncodedData);

            DecodedData = new Rgba32[info.Width * info.Height * 4];
            DecodeTexture(EncodedData, info, DecodedData);

            us.Position += info.Size;
        }
    }

    // CWDH
    public class CWDH {
        public u16 FirstGlyphCode { get; set; }
        public u16 LastGlyphCode { get; set; }
        public Dictionary<u16, CWDHEntry> Entries { get; set; }

        public CWDH() {
            Entries = [];
        }

        public u32 Read(UltraStream us) {
            FirstGlyphCode = us.ReadValue<u16>();
            LastGlyphCode = us.ReadValue<u16>();
            var nextCWDH = us.ReadValue<u32>();

            for (u16 cc = FirstGlyphCode; cc <= LastGlyphCode; cc++) {
                var width = new CWDHEntry();
                width.Read(us);
                Entries.Add(cc, width);
            }

            return nextCWDH;
        }

        public void Write(UltraStream us) {
            us.WriteValue(FirstGlyphCode);
            us.WriteValue(LastGlyphCode);
            us.Skip(4);

            for (u16 cc = FirstGlyphCode; cc <= LastGlyphCode; cc++) {
                Entries[cc].Write(us);
            }
        }
    }

    public class CWDHEntry {
        public s8 LeftSpacing { get; set; }
        public s8 GlyphWidth { get; set; }
        public s8 CharacterWidth { get; set; }

        public void Read(UltraStream us) {
            LeftSpacing = us.ReadValue<s8>();
            GlyphWidth = us.ReadValue<s8>();
            CharacterWidth = us.ReadValue<s8>();
        }

        public void Write(UltraStream us) {
            us.WriteValue(LeftSpacing);
            us.WriteValue(GlyphWidth);
            us.WriteValue(CharacterWidth);
        }
    }

    // CMAP
    public class CMAP {
        public u16 FirstCharacterCode { get; set; }
        public u16 LastCharacterCode { get; set; }
        public CharacterMapType MapType { get; set; }
        public Dictionary<u16, s16> Entries { get; set; }

        public CMAP() {
            Entries = [];
        }

        public u32 Read(UltraStream us) {
            FirstCharacterCode = us.ReadValue<u16>();
            LastCharacterCode = us.ReadValue<u16>();
            MapType = us.ReadValue<CharacterMapType>();
            us.Skip(2);
            var nextCMAP = us.ReadValue<u32>();

            switch (MapType) {
                case CharacterMapType.Direct:
                    ReadDirectMap(us);
                    break;
                case CharacterMapType.Table:
                    ReadTableMap(us);
                    break;
                case CharacterMapType.Scan:
                    ReadScanMap(us);
                    break;
                default:
                    throw new FormatException($"Unsupported CMAP type {MapType}.");
            }

            return nextCMAP;
        }

        public void Write(UltraStream us) {
            us.WriteValue(FirstCharacterCode);
            us.WriteValue(LastCharacterCode);
            us.WriteValue(MapType);
            us.Skip(2);
            us.Skip(4);

            switch (MapType) {
                case CharacterMapType.Direct:
                    WriteDirectMap(us);
                    break;
                case CharacterMapType.Table:
                    WriteTableMap(us);
                    break;
                case CharacterMapType.Scan:
                    WriteScanMap(us);
                    break;
                default:
                    throw new FormatException($"Unsupported CMAP type {MapType}.");
            }
        }

        private void ReadDirectMap(UltraStream us) {
            var c = us.ReadValue<u16>();

            for (u16 i = FirstCharacterCode; i <= LastCharacterCode; i++) {
                Entries.Add(c++, (s16)i);
            }
        }

        private void ReadTableMap(UltraStream us) {
            for (u16 i = FirstCharacterCode; i <= LastCharacterCode; i++) {
                var g = us.ReadValue<s16>();
                Entries.Add(i, g);
            }
        }

        private void ReadScanMap(UltraStream us) {
            var num = us.ReadValue<u16>();

            for (u16 i = 0; i < num; i++) {
                var c = us.ReadValue<u16>();
                var g = us.ReadValue<s16>();
                Entries.Add(c, g);
            }
        }

        private void WriteDirectMap(UltraStream us) {
            us.WriteValue(Entries.Keys.Min());
        }

        private void WriteTableMap(UltraStream us) {
            for (u16 i = FirstCharacterCode; i <= LastCharacterCode; i++) {
                us.WriteValue(Entries[i]);
            }
        }

        private void WriteScanMap(UltraStream us) {
            us.WriteValue((u16)Entries.Count);

            foreach (var entry in Entries) {
                us.WriteValue(entry.Key);
                us.WriteValue(entry.Value);
            }
        }
    }
}