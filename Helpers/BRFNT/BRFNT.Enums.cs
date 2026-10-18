namespace RevoFontEditor.Formats;

public partial class BRFNT {
    public enum FontTextureType : u8 {
        Glyph = 0x00,
        NNGC = 0x01,
    }

    public enum FontEncodingType : u8 {
        UTF8 = 0x00,
        UTF16 = 0x01,
        ShiftJIS = 0x02,
        CP1252 = 0x03,
    }

    public enum CharacterMapType : u16 {
        Direct = 0x00,
        Table = 0x01,
        Scan = 0x02,
    }

    public enum TextureFormat : u16 {
        // Bits per Pixel | Block Width | Block Height | Block Size | Type / Description
        I4 = 0x0,      //  4 | 8 | 8 | 32 | grey
        I8 = 0x1,      //  8 | 8 | 8 | 32 | grey
        IA4 = 0x2,     //  8 | 8 | 4 | 32 | grey + alpha
        IA8 = 0x3,     // 16 | 4 | 4 | 32 | grey + alpha
        RGB565 = 0x4,  // 16 | 4 | 4 | 32 | color
        RGB5A3 = 0x5,  // 16 | 4 | 4 | 32 | color + alpha
        RGBA32 = 0x6,  // 32 | 4 | 4 | 64 | color + alpha
        /*
        C4 = 0x8,      //  4 | 8 | 8 | 32 | palette choices (IA8, RGB565, RGB5A3)
        C8 = 0x9,      //  8 | 8 | 4 | 32 | palette choices (IA8, RGB565, RGB5A3)
        C14X2 = 0xA,   // 16 | 4 | 4 | 32 | palette (IA8, RGB565, RGB5A3) NOTE: only 14 bits are used per pixel
        */
        CMPR = 0xE,    //  4 | 8 | 8 | 32 | mini palettes in each block, RGB565 or transparent.
    }
}