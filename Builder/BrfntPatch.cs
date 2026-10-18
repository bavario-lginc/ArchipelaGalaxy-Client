using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using RevoFontEditor.Formats;
using SixLabors.ImageSharp.PixelFormats;
using Tmds.DBus.Protocol;
using SixLabors.ImageSharp;

class BrfntPatch
{
    public enum ActionType
    {
        AddTexture
    }
    public ActionType action;
    public string? external_path;
    public int cwdh_index;
    public Dictionary<u16, BRFNT.CWDHEntry> cwdh_entries = new();
    public int cmap_index;
    public Dictionary<u16, s16> cmap_entries = new();

    public bool Apply (BRFNT brfnt, string pathToExternal)
    {
        switch (action)
        {
            case ActionType.AddTexture:{
                if (external_path is null) 
                    return false;
                using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(Path.Combine(pathToExternal, external_path));
                var tex = new BRFNT.TGPLTexture(brfnt.GlyphTextureTable);
                var info = new BRFNT.ImageInfo()
                {
                    Size = BRFNT.GetEncodedTextureSize(brfnt.GlyphTextureTable.Format, brfnt.GlyphTextureTable.Width, brfnt.GlyphTextureTable.Height),
                    Width = brfnt.GlyphTextureTable.Width,
                    Height = brfnt.GlyphTextureTable.Height,
                    Format = brfnt.GlyphTextureTable.Format
                };
                if (img.Width != info.Width || img.Height != info.Height) 
                    return false;
                tex.Encode(img, info);
                brfnt.GlyphTextureTable.Textures.Add(Path.GetFileName(external_path), tex);

                // CWDH
                var cwdh = brfnt.CharacterWidthTables[cwdh_index];
                foreach ((u16 index, BRFNT.CWDHEntry entry) in cwdh_entries)
                {
                    if (index < cwdh.FirstGlyphCode) 
                        cwdh.FirstGlyphCode = index;
                    if (index > cwdh.LastGlyphCode) 
                        cwdh.LastGlyphCode = index;
                    cwdh.Entries.Add(index, entry);
                }
                // Padding
                for (u16 i = cwdh.FirstGlyphCode; i < cwdh.LastGlyphCode; i++)
                {
                    if (!cwdh.Entries.ContainsKey(i))
                    {
                        cwdh.Entries.Add(i, new BRFNT.CWDHEntry());
                    }
                }

                // CMAP
                var cmap = brfnt.CharacterMaps[cmap_index];
                foreach ((u16 index, s16 entry) in cmap_entries)
                {
                    if (index < cmap.FirstCharacterCode) 
                        cmap.FirstCharacterCode = index;
                    if (index > cmap.LastCharacterCode) 
                        cmap.LastCharacterCode = index;
                    cmap.Entries.Add(index, entry);
                }
                // Padding
                for (u16 i = cmap.FirstCharacterCode; i < cmap.LastCharacterCode; i++)
                {
                    if (!cmap.Entries.ContainsKey(i))
                    {
                        cmap.Entries.Add(i, -1);
                    }
                }
                brfnt.WriteToJson("test");
                break;
            }
        }
        return true;
    }
}