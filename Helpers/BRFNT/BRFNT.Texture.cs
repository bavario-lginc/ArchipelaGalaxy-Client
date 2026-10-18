using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace RevoFontEditor.Formats;

public partial class BRFNT {
    public class ImageInfo {
        public required s32 Size { get; init; }
        public required u16 Width { get; init; }
        public required u16 Height { get; init; }
        public required TextureFormat Format { get; init; }
    }

    private static readonly s32[] TileSize = [8, 4, 4, 4, 4, 4, 4, 0, 8, 4, 4, 0, 0, 0, 8];
    private static readonly s32[] BlocksPerPixel = [4, 8, 8, 16, 16, 16, 32, 0, 4, 8, 16, 0, 0, 0, 4];

    public static s32 GetEncodedTextureSize(TextureFormat format, s32 width, s32 height) {
        var formatIdx = (u32)format;
        var tileSize = TileSize[formatIdx];

        width = (width + tileSize - 1) & ~(tileSize - 1);
        height = (height + tileSize - 1) & ~(tileSize - 1);

        return width * height * BlocksPerPixel[formatIdx] / 8;
    }

    private static u32 GetTexturePixelOffset(ImageInfo info, u32 x, u32 y) {
        return info.Width * y + x;
    }

    public static void EncodeTexture(Rgba32[] data, ImageInfo info, u8[] output) {
        switch (info.Format) {
            case TextureFormat.I4: EncodeI4(data, info, output); return;
            case TextureFormat.I8: EncodeI8(data, info, output); return;
            case TextureFormat.IA4: EncodeIA4(data, info, output); return;
            case TextureFormat.IA8: EncodeIA8(data, info, output); return;
            // case TextureFormat.RGB565: DecodeRGB565(data, info, output); return;
            case TextureFormat.RGB5A3: EncodeRGB5A3(data, info, output); return;
            // case TextureFormat.RGBA32: DecodeRGBA32(data, info, output); return;
            // case TextureFormat.CMPR: DecodeCMPR(data, info, output); return;
        }

        throw new FormatException($"Unsupported texture format {info.Format}.");
    }

    private static void EncodeI4(Rgba32[] data, ImageInfo info, u8[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 8) {
            for (u32 xb = 0; xb < info.Width; xb += 8) {
                for (u32 y = 0; y < 8; y++) {
                    for (u32 x = 0; x < 8; x += 2) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        var color0 = data[offset];
                        var color1 = data[offset + 1];
                        var value0 = (u8)((color0.R + color0.G + color0.B + color0.A) / 4);
                        var value1 = (u8)((color1.R + color1.G + color1.B + color1.A) / 4);

                        output[idx++] = (u8)((value0 & 0xF0) | (value1 >> 4));
                    }
                }
            }
        }
    }

    private static void EncodeI8(Rgba32[] data, ImageInfo info, u8[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 8) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 8; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        var color = data[offset];
                        var value = (u8)((color.R + color.G + color.B + color.A) / 4);

                        output[idx++] = value;
                    }
                }
            }
        }
    }

    private static void EncodeIA4(Rgba32[] data, ImageInfo info, u8[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 8) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 8; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        var color = data[offset];
                        var value = (u8)((color.R + color.G + color.B) / 3);

                        output[idx++] = (u8)((color.A & 0xF0) | (value >> 4));
                    }
                }
            }
        }
    }

    private static void EncodeIA8(Rgba32[] data, ImageInfo info, u8[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 4; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        var color = data[offset];
                        var value = (u8)((color.R + color.G + color.B) / 3);

                        output[idx++] = value;
                        output[idx++] = color.A;
                    }
                }
            }
        }
    }

    private static void EncodeRGB5A3(Rgba32[] data, ImageInfo info, u8[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 4; x++) {
                        if (xb + x >= info.Width || yb + y >= info.Height)
                            continue;

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        var color = data[offset];

                        u16 pixel = 0;
                        if (color.A >= 0xE0) {
                            pixel = 0x8000;
                            pixel |= (u16)((color.R >> 3) << 10);
                            pixel |= (u16)((color.G >> 3) << 5);
                            pixel |= (u16)(color.B >> 3);
                        }
                        else {
                            pixel |= (u16)((color.A >> 5) << 12);
                            pixel |= (u16)((color.R >> 4) << 8);
                            pixel |= (u16)((color.G >> 4) << 4);
                            pixel |= (u16)(color.B >> 4);
                        }

                        output[idx++] = (u8)(pixel >> 8);
                        output[idx++] = (u8)pixel;
                    }
                }
            }
        }
    }

    public static void DecodeTexture(u8[] data, ImageInfo info, Rgba32[] output) {
        switch (info.Format) {
            case TextureFormat.I4: DecodeI4(data, info, output); return;
            case TextureFormat.I8: DecodeI8(data, info, output); return;
            case TextureFormat.IA4: DecodeIA4(data, info, output); return;
            case TextureFormat.IA8: DecodeIA8(data, info, output); return;
            case TextureFormat.RGB565: DecodeRGB565(data, info, output); return;
            case TextureFormat.RGB5A3: DecodeRGB5A3(data, info, output); return;
            case TextureFormat.RGBA32: DecodeRGBA32(data, info, output); return;
            case TextureFormat.CMPR: DecodeCMPR(data, info, output); return;
        }

        throw new FormatException($"Unsupported texture format {info.Format}.");
    }

    private static void DecodeI4(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 8) {
            for (u32 xb = 0; xb < info.Width; xb += 8) {
                for (u32 y = 0; y < 8; y++) {
                    for (u32 x = 0; x < 8; x += 2) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u8 pixel = data[idx++];
                        u8 px0 = (u8)(((pixel & 0xF0) >> 4) * 0x11);
                        u8 px1 = (u8)((pixel & 0x0F) * 0x11);

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        output[offset].R = px0;
                        output[offset].G = px0;
                        output[offset].B = px0;
                        output[offset].A = px0;
                        output[offset + 1].R = px1;
                        output[offset + 1].G = px1;
                        output[offset + 1].B = px1;
                        output[offset + 1].A = px1;
                    }
                }
            }
        }
    }

    private static void DecodeI8(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 8) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 8; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u8 pixel = data[idx++];

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        output[offset].R = pixel;
                        output[offset].G = pixel;
                        output[offset].B = pixel;
                        output[offset].A = pixel;
                    }
                }
            }
        }
    }

    private static void DecodeIA4(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 8) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 8; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u8 pixel = data[idx++];
                        u8 alpha = (u8)((pixel & 0xF0) >> 4);
                        u8 lum = (u8)(pixel & 0x0F);

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        output[offset].R = (u8)(lum * 0x11);
                        output[offset].G = (u8)(lum * 0x11);
                        output[offset].B = (u8)(lum * 0x11);
                        output[offset].A = (u8)(alpha * 0x11);
                    }
                }
            }
        }
    }

    private static void DecodeIA8(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 4; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u8 pixel0 = data[idx++];
                        u8 pixel1 = data[idx++];

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        output[offset].R = pixel1;
                        output[offset].G = pixel1;
                        output[offset].B = pixel1;
                        output[offset].A = pixel0;
                    }
                }
            }
        }
    }

    private static void DecodeRGB565(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 4; x++) {
                        if ((xb + x >= info.Width) || (yb + y >= info.Height))
                            continue;

                        u16 pixel = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(data, idx));
                        idx += 2;

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        DecodeRGB565Color(pixel, output, offset);
                    }
                }
            }
        }
    }

    private static void DecodeRGB5A3(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 4; x++) {
                        if (xb + x >= info.Width || yb + y >= info.Height)
                            continue;

                        u16 pixel = BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(data, idx));
                        idx += 2;

                        u8 r, g, b, a;

                        if ((pixel & 0x8000) != 0) {
                            a = 0xFF;
                            r = (u8)(((pixel >> 10) & 0x1F) * 0x8);
                            g = (u8)(((pixel >> 5) & 0x1F) * 0x8);
                            b = (u8)((pixel & 0x1F) * 0x8);
                        }
                        else {
                            a = (u8)(((pixel >> 12) & 0x7) * 0x20);
                            r = (u8)(((pixel >> 8) & 0xF) * 0x11);
                            g = (u8)(((pixel >> 4) & 0xF) * 0x11);
                            b = (u8)((pixel & 0xF) * 0x11);
                        }

                        u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                        output[offset].R = r;
                        output[offset].G = g;
                        output[offset].B = b;
                        output[offset].A = a;
                    }
                }
            }
        }
    }

    private static void DecodeRGBA32(u8[] data, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;
        
        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                for (s32 colorBlock = 0; colorBlock <= 1; colorBlock++) {
                    for (u32 y = 0; y < 4; y++) {
                        for (u32 x = 0; x < 4; x++) {
                            if ((xb + x >= info.Width) || (yb + y >= info.Height))
                                continue;

                            u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                            if (colorBlock == 1) {
                                output[offset].G = data[idx++];
                                output[offset].B = data[idx++];
                            }
                            else {
                                output[offset].A = data[idx++];
                                output[offset].R = data[idx++];
                            }
                        }
                    }
                }
            }
        }
    }

    private static void DecodeCMPR(u8[] data, ImageInfo info, Rgba32[] output) {
        var buffer = new u8[output.Length];
        s32 idx = 0;

        for (u32 y = 0; y < info.Height / 4; y += 2) {
            for (u32 x = 0; x < info.Width / 4; x += 2) {
                for (u32 dy = 0; dy < 2; dy++) {
                    for (u32 dx = 0; dx < 2; dx++) {
                        if (4 * (x + dx) >= info.Width || 4 * (y + dy) >= info.Height)
                            continue;

                        var source = data.AsSpan(idx, 8);
                        var target = buffer.AsSpan((s32)(8 * ((y + dy) * info.Width / 4 + x + dx)), 8);
                        idx += 8;

                        source.CopyTo(target);
                    }
                }
            }
        }

        for (u32 i = 0; i < info.Width * info.Height / 2; i += 8) {
            (buffer[i + 1], buffer[i]) = (buffer[i], buffer[i + 1]);
            (buffer[i + 2], buffer[i + 3]) = (buffer[i + 3], buffer[i + 2]);

            buffer[i + 4] = S3TC1ReverseU8(buffer[i + 4]);
            buffer[i + 5] = S3TC1ReverseU8(buffer[i + 5]);
            buffer[i + 6] = S3TC1ReverseU8(buffer[i + 6]);
            buffer[i + 7] = S3TC1ReverseU8(buffer[i + 7]);
        }

        DecompressDXT1(buffer, info, output);

        static u8 S3TC1ReverseU8(u8 b) {
            u8 b1 = (u8)(b & 0x3);
            u8 b2 = (u8)(b & 0xC);
            u8 b3 = (u8)(b & 0x30);
            u8 b4 = (u8)(b & 0xC0);

            return (u8)((b1 << 6) | (b2 << 2) | (b3 >> 2) | (b4 >> 6));
        }
    }

    private static void DecompressDXT1(u8[] buffer, ImageInfo info, Rgba32[] output) {
        s32 idx = 0;

        for (u32 yb = 0; yb < info.Height; yb += 4) {
            for (u32 xb = 0; xb < info.Width; xb += 4) {
                u16 color0 = BitConverter.ToUInt16(buffer, idx);
                u16 color1 = BitConverter.ToUInt16(buffer, idx + 2);
                u32 bits = BitConverter.ToUInt32(buffer, idx + 4);
                idx += 8;

                var colorTable = new Rgba32[4];
                DecodeRGB565Color(color0, colorTable, 0);
                DecodeRGB565Color(color1, colorTable, 1);

                colorTable[3].R = (u8)((colorTable[0].R + 2 * colorTable[1].R + 1) / 3);
                colorTable[3].G = (u8)((colorTable[0].G + 2 * colorTable[1].G + 1) / 3);
                colorTable[3].B = (u8)((colorTable[0].B + 2 * colorTable[1].B + 1) / 3);

                if (color0 > color1) {
                    colorTable[2].R = (u8)((2 * colorTable[0].R + colorTable[1].R + 1) / 3);
                    colorTable[2].G = (u8)((2 * colorTable[0].G + colorTable[1].G + 1) / 3);
                    colorTable[2].B = (u8)((2 * colorTable[0].B + colorTable[1].B + 1) / 3);
                    colorTable[2].A = 0xFF;
                    colorTable[3].A = 0xFF;
                }
                else {
                    colorTable[2].R = (u8)((colorTable[0].R + colorTable[1].R + 1) / 2);
                    colorTable[2].G = (u8)((colorTable[0].G + colorTable[1].G + 1) / 2);
                    colorTable[2].B = (u8)((colorTable[0].B + colorTable[1].B + 1) / 2);
                    colorTable[2].A = 0xFF;
                    colorTable[3].A = 0x00;
                }

                for (u32 y = 0; y < 4; y++) {
                    for (u32 x = 0; x < 4; x++) {
                        if (((xb + x) < info.Width) && ((yb + y) < info.Height)) {
                            u32 offset = GetTexturePixelOffset(info, xb + x, yb + y);
                            output[offset] = colorTable[bits & 0x3u];
                        }

                        bits >>= 2;
                    }
                }
            }
        }
    }

    private static void DecodeRGB565Color(u16 color, Rgba32[] output, u32 offset) {
        u8 r = (u8)((color & 0xF100) >> 11);
        u8 g = (u8)((color & 0x7E0) >> 5);
        u8 b = (u8)(color & 0x1F);

        output[offset].R = (u8)((r << 3) | (r >> 2));
        output[offset].G = (u8)((g << 2) | (g >> 4));
        output[offset].B = (u8)((b << 3) | (b >> 2));
        output[offset].A = 0xFF;
    }
}
