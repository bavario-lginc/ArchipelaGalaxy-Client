using RevoFontEditor.System;

namespace RevoFontEditor.Formats;

public class SectionInfo {
    public u32 Magic { get; private set; }
    public u32 Size { get; private set; }
    public u32 Position { get; private set; }

    public SectionInfo() {

    }

    public void Read(UltraStream us) {
        Position = (u32)us.Position;
        Magic = us.ReadValue<u32>();
        Size = us.ReadValue<u32>();
    }

    public void Write(UltraStream us, u32 magic, u32 size) {
        Position = (u32)us.Position;
        us.WriteValue(Magic = magic);
        us.WriteValue(Size = size);
    }

    public void SetSize(UltraStream us, u32 size) {
        using (us.TemporarySeek(Position + 4)) {
            us.WriteValue(Size = size);
        }
    }
}
